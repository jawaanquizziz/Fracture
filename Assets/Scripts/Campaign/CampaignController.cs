using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace Valmon.Campaign
{
    public enum CampaignScreen { Intro, Menu, Explore, Capture, ChoosePartner, Battle, StageClear, Result, Lobby, Countdown, RoundResult, MatchResult }

    public partial class CampaignController : MonoBehaviour
    {
        public CampaignCatalog catalog;
        public CampaignScreen ScreenState { get; private set; }
        public CampaignRun Run { get; private set; }
        public CampaignWorld World { get; private set; }
        public PlayerRole Role { get; private set; }
        public string LastMessage => message;
        public bool IsFinalBattle => finalBattle;
        private System.Random random;
        private Camera view;
        private AudioSource sound;
        private AudioSource menuAudio, gameAudio, battleAudio;
        private bool introRecording;
        private AudioClip cue;
        private VideoPlayer video;
        private RenderTexture videoTexture;
        private float introStarted, videoWait;
        private int menuStep, selectedLevel, selectedCharacter, selectedStarter, unlocked;
        private bool settings, paused, finalBattle, tamerTurn, outcomeTamer;
        private float yaw, pitch = 24, verticalSpeed, corruptionEnergy = 100, aiClock, turnDelay, captureClock, animationClock;
        private float volume = .7f;
        private Vector3 corruptorPosition;
        private float corruptorYaw, corruptorPitch = 27f;
        private bool captureIntervened, attackByTamer;
        private string message = "", resultReason = "";
        private WildCreature selected, encounter, aiTarget;
        private Queue<Vector3> aiRoute = new Queue<Vector3>();
        private GameObject captureVisual;
        private Vector3 captureOrigin;
        private int partyIndex;
        private OwnedCreature opponent;
        private readonly List<OwnedCreature> bossTeam = new List<OwnedCreature>();
        private readonly string[] mapNames = { "Verdant Ruins", "Ember Fortress", "Tidal Sanctuary" };
        private readonly string[] starterIds = { "bulbasaur", "charmander", "squirtle" };
        private bool Active => ScreenState == CampaignScreen.Explore || ScreenState == CampaignScreen.Capture || ScreenState == CampaignScreen.ChoosePartner || ScreenState == CampaignScreen.Battle;
        private bool HumanTurn => tamerTurn == (Role == PlayerRole.Tamer);

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Application.runInBackground = true;
            random = new System.Random();
            unlocked = Mathf.Clamp(PlayerPrefs.GetInt("VALMON.UnlockedStage", 0), 0, 2);
            volume = PlayerPrefs.GetFloat("VALMON.Volume", .7f); AudioListener.volume = volume;
            playerName=CleanPlayerName(PlayerPrefs.GetString("VALMON.PlayerName","Player"));
            GameObject cameraObject = new GameObject("Campaign Camera", typeof(Camera), typeof(AudioListener));
            view = cameraObject.GetComponent<Camera>(); view.tag = "MainCamera";
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.025f, .045f, .08f);
            view.nearClipPlane = .08f; view.farClipPlane = 600;
            sound = gameObject.AddComponent<AudioSource>();
            sound.playOnAwake = false;
            float[] wave = new float[4096];
            for (int i = 0; i < wave.Length; i++) wave[i] = Mathf.Sin(i * 2 * Mathf.PI * 640 / 44100) * (1f - (float)i / wave.Length) * .14f;
            cue = AudioClip.Create("Rift cue", wave.Length, 1, 44100, false); cue.SetData(wave, 0);
            if (catalog == null || catalog.maps.Length != 3 || catalog.characters.Length != 4)
            { ScreenState = CampaignScreen.Result; resultReason = "Campaign data is missing. Rebuild via Tools > VALMON > Build Campaign."; return; }
            World = new CampaignWorld(catalog, view);
            ScreenState = CampaignScreen.Intro;
            World.ShowStadium();
            AudioClip menuClip = catalog.menuMusic != null ? catalog.menuMusic : catalog.introMusic;
            menuAudio = gameObject.AddComponent<AudioSource>(); menuAudio.loop = true; menuAudio.volume = .2f; menuAudio.clip = menuClip;
            if (menuAudio.clip != null) menuAudio.Play();

            gameAudio = gameObject.AddComponent<AudioSource>(); gameAudio.loop = true; gameAudio.volume = 0f; gameAudio.clip = catalog.gameSoundtrack;
            if (gameAudio.clip != null) gameAudio.Play();

            battleAudio = gameObject.AddComponent<AudioSource>(); battleAudio.loop = false; battleAudio.volume = .9f; battleAudio.clip = catalog.battleMusic;

            if (!string.IsNullOrEmpty(catalog.introFile)) StartVideo();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (System.Environment.GetCommandLineArgs().Contains("-valmonSmoke")) StartCoroutine(SmokeRunner());
            StartNetworkSmokeIfRequested();
            StartPresentationCaptureIfRequested();
#endif
        }

        private void StartVideo()
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            string path = Path.Combine(Application.streamingAssetsPath, catalog.introFile);
            if (!File.Exists(path)) { OpenMenu(); return; }
            videoTexture = new RenderTexture(1280, 720, 0); videoTexture.Create();
            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false; video.isLooping = false;
            video.renderMode = VideoRenderMode.RenderTexture; video.targetTexture = videoTexture;
            video.source = VideoSource.Url; video.url = path;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.SetTargetAudioSource(0, sound);
            video.prepareCompleted += OnVideoPrepared;
            video.loopPointReached += OnVideoEnded;
            video.errorReceived += OnVideoError;
            video.Prepare();
        }
        private void OnVideoPrepared(VideoPlayer source) { if (ScreenState == CampaignScreen.Intro) source.Play(); }
        private void OnVideoEnded(VideoPlayer source) => OpenMenu();
        private void OnVideoError(VideoPlayer source, string error) { Debug.LogWarning("[VALMON] Intro skipped: " + error); OpenMenu(); }

        public void OpenMenu()
        {
            if (video != null) video.Stop();
            RemoveCaptureVisual();
            paused = false; settings = false; finalBattle = false;
            ScreenState = CampaignScreen.Menu; menuStep = 0;
            World?.ShowMenu();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        private void Update()
        {
            // Always pump networking first so packets apply before game logic.
            NetTick();
            // Smooth dynamic music crossfades based on game state
            bool isMenu = ScreenState == CampaignScreen.Menu || ScreenState == CampaignScreen.Lobby || (ScreenState == CampaignScreen.Intro && video == null);
            bool isBattle = ScreenState == CampaignScreen.Battle;
            bool isGameplay = !isMenu && !isBattle && Active && ScreenState != CampaignScreen.Result && ScreenState != CampaignScreen.StageClear;

            if (menuAudio != null) menuAudio.volume = Mathf.MoveTowards(menuAudio.volume, isMenu ? .8f : 0f, Time.unscaledDeltaTime * 1.2f);
            if (gameAudio != null) gameAudio.volume = Mathf.MoveTowards(gameAudio.volume, isGameplay ? .8f : 0f, Time.unscaledDeltaTime * 1.2f);
            // battleAudio no longer tied to state volume - played once per battle via BeginBattleView; stop it when state leaves Battle.
            if (battleAudio != null && !isBattle && battleAudio.isPlaying) battleAudio.Stop();
            if (netMode == NetMode.Client) { NetClientFrame(); return; }
            if (ScreenState == CampaignScreen.Menu || ScreenState == CampaignScreen.Lobby) World?.TickPresentation(Time.unscaledDeltaTime);
            if (ScreenState == CampaignScreen.Lobby) return;
            if (ScreenState == CampaignScreen.Intro)
            {
                videoWait += Time.unscaledDeltaTime;
                bool skip = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
                World?.TickPresentation(Time.unscaledDeltaTime, Mathf.Clamp01(videoWait / 12f));
                if (!introRecording && ((skip && Time.unscaledTime - introStarted > .3f) || videoWait > 12.5f)) OpenMenu();
                return;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Active) TogglePause();
                else if (ScreenState == CampaignScreen.Menu) { settings = false; menuStep = Mathf.Max(0, menuStep - 1); }
            }
            if (!Active || paused || Run == null) return;
            TickAbilities();
            if(!Active) return;
            if (!finalBattle)
            {
                Run.seconds = Mathf.Max(0, Run.seconds - Time.deltaTime);
                if (Run.seconds <= 0) { Lose("The 8-minute catching timer expired."); return; }
            }
            corruptionEnergy = Mathf.Min(100, corruptionEnergy + Time.deltaTime * 6);
            switch (ScreenState)
            {
                case CampaignScreen.Explore:
                    if (Role == PlayerRole.Tamer) { MoveTamer(); if(netMode==NetMode.Host) MoveRemoteWarden(); }
                    else
                    {
                        // In multiplayer the tamer position is replicated from the host; skip AI.
                        if (netMode == NetMode.Host) MoveRemoteTamer(); else MoveAITamer();
                        MoveCorruptorCamera();
                    }
                    World?.TickAmbient(Time.deltaTime);
                    ReadAbilityKeys();
                    UpdateCamera();
                    if (Role == PlayerRole.Tamer)
                    {
                        selected = NearestWild();
                        // Only tick AI corruption in single-player; in multiplayer the real Corruptor player drives it.
                        if (netMode == NetMode.None)
                        {
                            aiClock -= Time.deltaTime;
                            if (aiClock <= 0)
                            {
                                aiClock = random.Next(12, 21);
                                WildCreature[] clean = World.wild.Where(w => !w.captured && !w.corrupted).ToArray();
                                if (clean.Length > 0) TryCorrupt(clean[random.Next(clean.Length)]);
                            }
                        }
                        if (Keyboard.current != null && (Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame) && selected != null) BeginCapture(selected);
                    }
                    if (Role == PlayerRole.Corruptor) ReadCorruptorClick();
                    break;
                case CampaignScreen.Capture:
                    if (Role == PlayerRole.Corruptor) { MoveCorruptorCamera(); ReadAbilityKeys(); ReadCorruptorClick(); }
                    else if (netMode == NetMode.Host) MoveRemoteWarden();
                    UpdateCapture(); World?.TickAmbient(Time.deltaTime); UpdateCamera(); break;
                case CampaignScreen.ChoosePartner:
                    if (Role == PlayerRole.Corruptor && netMode == NetMode.None) ChoosePartner(BestPartner(encounter.creature));
                    break;
                case CampaignScreen.Battle:
                    turnDelay -= Time.deltaTime;
                    if (netMode == NetMode.None && !HumanTurn && turnDelay <= 0)
                    {
                        OwnedCreature actor = tamerTurn ? Run.party[partyIndex] : opponent;
                        OwnedCreature target = tamerTurn ? opponent : Run.party[partyIndex];
                        ResolveTurn(BattleRules.BestMove(actor, target, random));
                    }
                    AnimateAttack();
                    break;
            }
        }

        public void StartCampaign(int stage, int character, PlayerRole role, string starter)
        {
            if (catalog.Find(starter) == null) throw new ArgumentException("Unknown starter");
            selectedCharacter = Mathf.Clamp(character, 0, catalog.characters.Length - 1);
            Role = role; Run = new CampaignRun();
            Run.party.Add(new OwnedCreature(catalog.Find(starter), 10));
            BeginLevel(Mathf.Clamp(stage, 0, netMode==NetMode.None?unlocked:2));
        }

        public void BeginLevel(int stage)
        {
            paused = false; finalBattle = false; settings = false;
            encounter = selected = aiTarget = null; opponent=null; bossTeam.Clear(); aiRoute.Clear(); RemoveCaptureVisual();
            Run.StartStage(stage); partyIndex = 0; corruptionEnergy = 100; aiClock = 13;
            netStageSeed = netMode == NetMode.Host ? unchecked(netMatchSeed + stage * 7919) : random.Next();
            try { World.LoadStage(Run, selectedCharacter, new System.Random(netStageSeed)); World.ShowCorruptor(netMode == NetMode.None ? (selectedCharacter + 1) % 4 : netCorruptorCharacter); }
            catch (Exception error) { Debug.LogException(error); ScreenState = CampaignScreen.Result; resultReason = "Map could not be loaded: " + error.Message; return; }
            ScreenState = CampaignScreen.Explore; yaw = World.StartingYaw; pitch = 27; verticalSpeed = 0;
            message = "Catch " + CampaignRun.RequiredCatches + " " + Run.Habitat + " Valmons. Your starter does not count toward the goal.";
            if (Role == PlayerRole.Corruptor)
            {
                World.controller.enabled = netMode == NetMode.Host;
                corruptorPosition = World.towerPosition;
                corruptorYaw = 0f;
                corruptorPitch = 27f;
            }
            UpdateCamera(true);
        }

        private WildCreature NearestWild()
        {
            WildCreature result = null; float distance = float.MaxValue;
            foreach (WildCreature creature in World.wild)
            {
                if (creature.captured) continue;
                float candidate = Vector3.Distance(World.tamer.transform.position, creature.root.transform.position);
                if (candidate < distance) { result = creature; distance = candidate; }
            }
            return result;
        }

        private void MoveTamer()
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current != null)
            {
                input.x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
                input.y = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
                if (Keyboard.current.rKey.wasPressedThisFrame) World.ResetPlayer();
            }
            if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                yaw = (yaw + delta.x * .22f) % 360f;
                pitch = Mathf.Clamp(pitch - delta.y * .18f, -85f, 85f);
            }
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y).normalized;
            float speed = TamerSpeed(Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
            if (World.controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed -= 20 * Time.deltaTime;
            World.controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (direction.sqrMagnitude > .01f) World.tamer.transform.rotation = Quaternion.Slerp(World.tamer.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 12);
            if (World.tamer.transform.position.y < -8) { World.ResetPlayer(); verticalSpeed = 0; }
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame && selected != null) BeginCapture(selected);
        }

        private void MoveAITamer()
        {
            if(Run.catches>=6) { FinishCaptureGoal(); return; }
            if (aiTarget == null || aiTarget.captured)
            {
                var choices=World.wild.Where(w=>!w.captured).OrderBy(w=>Vector3.Distance(w.root.transform.position,World.tamer.transform.position)+w.creature.level*.5f+(w.corrupted?8:0)).ToArray();
                if(choices.Length==0) { Lose("No wild Valmons remain."); return; }
                aiTarget=null;
                foreach(WildCreature candidate in choices)
                {
                    Queue<Vector3> path=World.RouteTo(candidate);
                    if(path.Count==0&&Vector3.Distance(World.tamer.transform.position,candidate.root.transform.position)>=3.4f) continue;
                    aiTarget=candidate; aiRoute=path; break;
                }
                if(aiTarget==null) { World.ResetPlayer(); World.controller.enabled=false; return; }
            }
            if (Vector3.Distance(World.tamer.transform.position, aiTarget.root.transform.position) < 3.4f)
            { BeginCapture(aiTarget); return; }
            if (aiRoute.Count == 0) { aiTarget = null; return; }
            Vector3 destination = aiRoute.Peek() + Vector3.up * .12f;
            Vector3 movement = destination - World.tamer.transform.position;
            World.tamer.transform.position = Vector3.MoveTowards(World.tamer.transform.position, destination, Time.deltaTime * 3.7f);
            movement.y = 0;
            if (movement.sqrMagnitude > .01f) World.tamer.transform.rotation = Quaternion.Slerp(World.tamer.transform.rotation, Quaternion.LookRotation(movement), Time.deltaTime * 9);
            if (Vector3.Distance(World.tamer.transform.position, destination) < .15f) aiRoute.Dequeue();
        }

        private void UpdateCamera(bool snap = false)
        {
            GameObject avatar=Role==PlayerRole.Tamer?World.tamer:World.warden;
            if(avatar==null) return;
            Vector3 focus=avatar.transform.position+Vector3.up*(CampaignWorld.PlayerHeight*.6f);
            Quaternion rotation=Quaternion.Euler(Role==PlayerRole.Tamer?pitch:corruptorPitch,Role==PlayerRole.Tamer?yaw:corruptorYaw,0);
            float distance=CampaignWorld.PlayerHeight*3.6f; Vector3 direction=rotation*Vector3.back;
            if(Physics.SphereCast(focus,.18f,direction,out RaycastHit hit,distance,1<<CampaignWorld.TerrainLayer)) distance=Mathf.Max(1.1f,hit.distance-.18f);
            Vector3 destination=focus+direction*distance;
            view.transform.position=snap?destination:Vector3.Lerp(view.transform.position,destination,1-Mathf.Exp(-8*Time.deltaTime)); view.transform.rotation=rotation;
        }
        private void MoveCorruptorCamera()
        {
            if(World.wardenController==null) return;
            Vector2 input=ReadMoveInput();
            if(Mouse.current!=null&&Mouse.current.rightButton.isPressed)
            {
                Vector2 delta=Mouse.current.delta.ReadValue();
                corruptorYaw=(corruptorYaw+delta.x*.22f)%360f;
                corruptorPitch=Mathf.Clamp(corruptorPitch-delta.y*.18f,-85f,85f);
            }
            Vector3 direction=Quaternion.Euler(0,corruptorYaw,0)*new Vector3(input.x,0,input.y);
            bool sprint=Keyboard.current!=null&&Keyboard.current.leftShiftKey.isPressed;
            if(World.wardenController.isGrounded&&wardenVertical<0) wardenVertical=-2;
            wardenVertical-=20*Time.deltaTime;
            World.wardenController.Move((direction*(sprint?7:5)+Vector3.up*wardenVertical)*Time.deltaTime);
            if(direction.sqrMagnitude>.01f) World.warden.transform.rotation=Quaternion.LookRotation(direction);
            if(World.warden.transform.position.y < -8) { World.ResetWarden(); wardenVertical=0; }
        }
        public bool TryRiftBurst(Vector3 center)
        {
            if (netMode == NetMode.Client) return NetCommand("burst", 0, center);
            if(World.warden==null||wardenCorruptCooldown>0||Vector3.Distance(World.warden.transform.position,center)>15) return false;
            const float cost = 55f, radius = 11f;
            if (corruptionEnergy < cost || (ScreenState != CampaignScreen.Explore && ScreenState != CampaignScreen.Capture)) return false;
            int affected = 0;
            foreach (WildCreature w in World.wild)
            {
                if (w == null || w.captured || w.corrupted || w.root == null) continue;
                if (Vector3.Distance(w.root.transform.position, center) <= radius)
                {
                    World.Corrupt(w); affected++;
                }
            }
            if (affected == 0) return false;
            corruptionEnergy -= cost;
            wardenCorruptCooldown=6; if(IsRoundMatch) roundSabotages+=affected;
            World.SpawnRiftBurst(center, radius);
            message = "Rift Burst corrupted " + affected + " creature" + (affected == 1 ? "" : "s") + "!";
            Cue(.4f);
            return true;
        }

        public bool TryCorrupt(WildCreature creature)
        {
            if (netMode == NetMode.Client) return creature != null && NetCommand("corrupt", World.wild.IndexOf(creature));
            if ((netMode!=NetMode.None || Role==PlayerRole.Corruptor) && (World.warden==null || creature==null || wardenCorruptCooldown>0 || Vector3.Distance(World.warden.transform.position,creature.root.transform.position)>12)) return false;
            if (creature == null || creature.captured || creature.corrupted || corruptionEnergy < 25 || (ScreenState != CampaignScreen.Explore && ScreenState != CampaignScreen.Capture)) return false;
            corruptionEnergy -= 25;
            if(netMode!=NetMode.None||Role==PlayerRole.Corruptor) wardenCorruptCooldown=2;
            if (IsRoundMatch) roundSabotages++;
            World.Corrupt(creature);
            message = creature.creature.species.displayName + " is corrupted. Its next capture requires a battle.";
            Cue(.7f);
            return true;
        }

        private void FinishCaptureGoal()
        {
            if (netMode == NetMode.Client) { if(Role == PlayerRole.Tamer) NetCommand("finish",0); return; }
            if (ScreenState != CampaignScreen.Explore || Run.catches < CampaignRun.RequiredCatches) return;
            CheckOutcome(true);
        }

        public bool BeginCapture(WildCreature creature)
        {
            if (netMode == NetMode.Client) return creature != null && NetCommand("catch", World.wild.IndexOf(creature));
            if (ScreenState != CampaignScreen.Explore || creature == null || creature.captured || Vector3.Distance(World.tamer.transform.position, creature.root.transform.position) > 4.4f) return false;
            if (!Run.SpendBall()) { CheckOutcome(); return false; }
            encounter = creature; captureClock = 0; captureIntervened = false;
            ScreenState = CampaignScreen.Capture;
            captureOrigin = World.tamer.transform.position + Vector3.up * 1.1f;
            captureVisual = World.Model(catalog.captureBall, null, captureOrigin, .35f);
            // Comet trail for the thrown Valmon Ball.
            TrailRenderer trail = captureVisual.AddComponent<TrailRenderer>();
            trail.time = .45f; trail.minVertexDistance = .05f;
            trail.widthCurve = AnimationCurve.EaseInOut(0f, .18f, 1f, 0f);
            trail.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            Color trailC = creature.corrupted ? new Color(1f, .55f, 1.3f) : new Color(1f, .85f, .35f);
            if (trail.material.HasProperty("_BaseColor")) trail.material.SetColor("_BaseColor", trailC);
            if (trail.material.HasProperty("_EmissionColor")) { trail.material.EnableKeyword("_EMISSION"); trail.material.SetColor("_EmissionColor", trailC * 1.6f); }
            message = "Capture channeling... the Corruptor can still intervene.";
            Cue(1.2f); return true;
        }

        private void UpdateCapture()
        {
            captureClock += Time.deltaTime;
            if (captureVisual != null)
            {
                float t = Mathf.Clamp01(captureClock / 1.3f);
                captureVisual.transform.position = Vector3.Lerp(captureOrigin, encounter.root.transform.position + Vector3.up, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.7f;
                captureVisual.transform.Rotate(0, 220 * Time.deltaTime, 80 * Time.deltaTime);
            }
            if (netMode == NetMode.None && Role == PlayerRole.Tamer && captureClock > 1 && !captureIntervened)
            {
                captureIntervened = true;
                if (random.NextDouble() < .55) TryCorrupt(encounter);
            }
            if (captureClock < 2.7f) return;
            RemoveCaptureVisual();
            if (encounter.corrupted)
            {
                encounter.creature.Heal();
                ScreenState = CampaignScreen.ChoosePartner;
                message = "Corruption interrupted the capture! Choose your battling Valmon.";
            }
            else FinishCatch();
        }

        public void ChoosePartner(int index)
        {
            if (netMode == NetMode.Client) { NetCommand("partner", index); return; }
            if (ScreenState != CampaignScreen.ChoosePartner || index < 0 || index >= Run.party.Count || Run.party[index].health <= 0) return;
            partyIndex = index; opponent = encounter.creature;
            foreach (OwnedCreature creature in Run.party) creature.ResetBattleEffects();
            BeginBattleView();
        }
        private int BestPartner(OwnedCreature enemy)
        {
            int best = 0; double score = -1;
            for (int i = 0; i < Run.party.Count; i++)
            {
                OwnedCreature creature = Run.party[i];
                double value = creature.health * creature.level * creature.species.moves.Max(m => Math.Max(.2f, BattleRules.Effectiveness(m.type, enemy.species)));
                if (value > score) { score = value; best = i; }
            }
            return best;
        }
        private void BeginBattleView()
        {
            netBattleSerial++;
            ScreenState = CampaignScreen.Battle;
            tamerTurn = Run.party[partyIndex].Speed >= opponent.Speed;
            turnDelay = 1.4f; animationClock = 0;
            if (battleAudio != null && battleAudio.clip != null) { battleAudio.Stop(); battleAudio.Play(); }
            World.ShowBattle(Run.party[partyIndex], opponent, Run.stage);
            message = finalBattle ? "Final duel. Defeat the Corruptor's entire team." : "Defeat the corrupted Valmon to complete your capture.";
        }
        public void PlayMove(int index)
        {
            if (netMode == NetMode.Client) { if (HumanTurn && turnDelay <= 0 && !paused) NetCommand("move", index); return; }
            if (ScreenState != CampaignScreen.Battle || !HumanTurn || turnDelay > 0 || paused) return;
            OwnedCreature actor = tamerTurn ? Run.party[partyIndex] : opponent;
            if (BattleRules.HasMoves(actor) && (index < 0 || index >= actor.pp.Length || actor.pp[index] <= 0)) return;
            ResolveTurn(index);
        }
        private void ResolveTurn(int move)
        {
            netTurnSerial++;
            OwnedCreature friendly = Run.party[partyIndex];
            message = BattleRules.Attack(tamerTurn ? friendly : opponent, tamerTurn ? opponent : friendly, move, random);
            attackByTamer = tamerTurn; animationClock = .42f; PlayAttackCue(tamerTurn ? catalog.captureAudio : catalog.corruptionAudio);
            bool allyLost = friendly.health <= 0, enemyLost = opponent.health <= 0;
            if (allyLost)
            {
                Cue(.5f);
                Run.party.RemoveAt(partyIndex); partyIndex = 0;
                if (Run.party.Count == 0) { Lose("Your last Valmon was defeated. The Tamer's team is empty."); return; }
            }
            if (finalBattle)
            {
                if (enemyLost) bossTeam.Remove(opponent);
                if (bossTeam.Count == 0) { WinCampaign(); return; }
                if (enemyLost) opponent = bossTeam[0];
                if (allyLost || enemyLost)
                {
                    partyIndex = BestPartner(opponent); BeginBattleView();
                    message = allyLost ? "A Tamer Valmon was lost. The next teammate enters!" : "One Corruptor Valmon defeated. The next one enters!";
                    return;
                }
            }
            else
            {
                if (allyLost)
                {
                    if(IsRoundMatch) roundStopped++;
                    message = friendly.species.displayName + " was lost from your team. The wild Valmon remains corrupted.";
                    ReturnToMap(); CheckOutcome(); return;
                }
                if (enemyLost)
                {
                    friendly.level = Math.Min(Run.MaxLevel, friendly.level + 2);
                    friendly.health = Math.Min(friendly.MaxHealth, friendly.health + friendly.MaxHealth / 4);
                    FinishCatch(); return;
                }
            }
            tamerTurn = !tamerTurn; turnDelay = 1.4f;
        }
        private void AnimateAttack()
        {
            if (animationClock <= 0 || World.battleTamer == null || World.battleEnemy == null) return;
            animationClock = Mathf.Max(0, animationClock - Time.deltaTime);
            float offset = Mathf.Sin((1 - animationClock / .42f) * Mathf.PI) * .7f;
            Vector3 lunge = (World.battleEnemyHome - World.battleTamerHome); lunge.y = 0; lunge = lunge.normalized;
            World.battleTamer.position = World.battleTamerHome + (attackByTamer ? lunge * offset : Vector3.zero);
            World.battleEnemy.position = World.battleEnemyHome - (!attackByTamer ? lunge * offset : Vector3.zero);
        }

        private void SwitchParty(int index)
        {
            if (netMode == NetMode.Client) { NetCommand("switch", index); return; }
            if (ScreenState != CampaignScreen.Battle || !HumanTurn || turnDelay > 0) return;
            SwitchPartyAs(index, Role);
        }
        private void SwitchPartyAs(int index, PlayerRole actorRole)
        {
            if (actorRole == PlayerRole.Tamer)
            {
                if (index == partyIndex || index < 0 || index >= Run.party.Count) return;
                partyIndex = index;
            }
            else
            {
                if (!finalBattle || index < 0 || index >= bossTeam.Count || bossTeam[index] == opponent) return;
                opponent = bossTeam[index];
            }
            World.ShowBattle(Run.party[partyIndex], opponent, Run.stage);
            netTurnSerial++;
            tamerTurn = !tamerTurn; turnDelay = 1.4f;
            message = "Switched Valmon. Switching consumes one turn.";
        }

        private void FinishCatch()
        {
            Vector3 at = encounter.root.transform.position + Vector3.up * .9f;
            encounter.captured = true; encounter.root.SetActive(false);
            World.SpawnCatchSparkle(at);
            Run.Catch(encounter.creature);
            message = "Caught " + encounter.creature.species.displayName + "! " + Run.catches + "/" + CampaignRun.RequiredCatches + " this level.";
            Cue(1.2f); ReturnToMap(); CheckOutcome();
        }
        private void ReturnToMap()
        {
            World.ResumeMap(); encounter = null; aiTarget = null; aiRoute.Clear();
            ScreenState = CampaignScreen.Explore; UpdateCamera(true);
        }
        private void CheckOutcome(bool finishGoal = false)
        {
            StageOutcome outcome = finishGoal ? StageOutcome.Cleared : Run.Outcome(ScreenState == CampaignScreen.Capture || ScreenState == CampaignScreen.ChoosePartner || ScreenState == CampaignScreen.Battle);
            if (outcome == StageOutcome.Cleared)
            {
                Cue(1.8f);
                if(IsRoundMatch) { FinishRound(true,"The Tamer caught five creatures before time ran out."); return; }
                ScreenState = CampaignScreen.StageClear;
                unlocked = Mathf.Max(unlocked, Mathf.Min(2, Run.stage + 1));
                PlayerPrefs.SetInt("VALMON.UnlockedStage", unlocked); PlayerPrefs.Save();
            }
            else if (outcome == StageOutcome.PartyLost) Lose("The Tamer has no Valmons left.");
            else if (outcome == StageOutcome.TimeExpired) Lose("The 8-minute timer expired.");
            else if (outcome == StageOutcome.NoBalls) Lose("Not enough Valmon Balls remain to reach " + CampaignRun.RequiredCatches + " catches.");
        }
        public void Advance()
        {
            if (netMode == NetMode.Client) return;
            if (ScreenState != CampaignScreen.StageClear) return;
            if (Run.stage < 2) BeginLevel(Run.stage + 1); else BeginFinal();
        }
        public void BeginFinal()
        {
            finalBattle = true; paused = false; bossTeam.Clear();
            foreach (OwnedCreature creature in Run.party) { creature.level = Math.Max(creature.level, 45); creature.Heal(); }
            foreach (SpeciesDefinition species in catalog.species.OrderBy(_ => random.Next()).Take(3)) bossTeam.Add(new OwnedCreature(species, 50));
            opponent = bossTeam[0]; partyIndex = BestPartner(opponent);
            BeginBattleView();
        }
        private void Lose(string reason)
        {
            if(IsRoundMatch) { FinishRound(false,reason); return; }
            RemoveCaptureVisual(); paused = false; outcomeTamer = false; resultReason = reason;
            ScreenState = CampaignScreen.Result; Cue(.5f);
        }
        private void WinCampaign()
        {
            outcomeTamer = true; paused = false;
            resultReason = "All three habitats cleared and the Corruptor's final team defeated.";
            ScreenState = CampaignScreen.Result; Cue(1.8f);
            PlayerPrefs.SetInt("VALMON.CampaignComplete", 1); PlayerPrefs.Save();
        }
        private AudioSource attackCueSource;
        private void PlayAttackCue(AudioClip clip)
        {
            if (clip == null) return;
            if (attackCueSource == null)
            {
                attackCueSource = gameObject.AddComponent<AudioSource>();
                attackCueSource.playOnAwake = false;
                attackCueSource.loop = false;
            }
            attackCueSource.Stop();
            attackCueSource.clip = clip;
            attackCueSource.volume = .9f;
            attackCueSource.pitch = 1f;
            attackCueSource.Play();
            // Stop at exactly half the clip's length so only the first half is heard.
            attackCueSource.SetScheduledEndTime(AudioSettings.dspTime + clip.length * 0.5);
        }
        private void Cue(float pitchValue)
        {
            if(sound==null) return;
            AudioClip supplied = null;
            if (pitchValue >= 1.5f) supplied = catalog.victoryMusic;
            else if (pitchValue >= 1.1f) supplied = catalog.captureAudio;
            else if (pitchValue <= .55f) supplied = catalog.battleLostAudio;
            else supplied = catalog.corruptionAudio;

            AudioClip clip = supplied != null ? supplied : cue;
            if(clip==null) return;
            sound.pitch = supplied != null ? 1f : pitchValue;
            sound.PlayOneShot(clip, 0.8f);
        }
        private void RemoveCaptureVisual() { if (captureVisual != null) Destroy(captureVisual); captureVisual = null; }
        private void OnDestroy()
        {
            NetDisconnect();
            if (video != null) { video.prepareCompleted -= OnVideoPrepared; video.loopPointReached -= OnVideoEnded; video.errorReceived -= OnVideoError; }
            World?.Dispose();
            if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); }
            if (cue != null) Destroy(cue);
        }
    }
}


