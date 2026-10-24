#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    // Explicit command-line validation only; never enabled in a normal game session.
    public partial class CampaignController
    {
        private readonly List<string> smokeResults = new List<string>();
        private readonly List<string> smokeErrors = new List<string>();
        private string smokeFolder;
        private IEnumerator SmokeRunner()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-valmonReport");
            smokeFolder = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.persistentDataPath, "CampaignSmoke");
            Directory.CreateDirectory(smokeFolder);
            int oldUnlock = PlayerPrefs.GetInt("VALMON.UnlockedStage", 0);
            int oldComplete = PlayerPrefs.GetInt("VALMON.CampaignComplete", 0);
            Application.logMessageReceived += SmokeLog;
            var stack = new Stack<IEnumerator>(); stack.Push(SmokeFlow());
            string failure = null;
            while (stack.Count > 0)
            {
                bool next = false; object value = null;
                try { next = stack.Peek().MoveNext(); if (next) value = stack.Peek().Current; }
                catch (Exception error) { failure = error.ToString(); break; }
                if (!next) { stack.Pop(); continue; }
                if (value is IEnumerator routine) stack.Push(routine);
                else yield return value;
            }
            Application.logMessageReceived -= SmokeLog;
            PlayerPrefs.SetInt("VALMON.UnlockedStage", oldUnlock); PlayerPrefs.SetInt("VALMON.CampaignComplete", oldComplete); PlayerPrefs.Save();
            if (smokeErrors.Count > 0) failure = (failure ?? "") + "\nRuntime errors:\n" + string.Join("\n", smokeErrors);
            smokeResults.Add(failure == null ? "PASS RUNTIME SMOKE COMPLETED" : "FAIL " + failure);
            File.WriteAllLines(Path.Combine(smokeFolder, "runtime-smoke.txt"), smokeResults);
            Debug.Log("[VALMON SMOKE] " + (failure == null ? "PASS" : failure));
            yield return null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(failure == null ? 0 : 1);
#else
            Application.Quit(failure == null ? 0 : 1);
#endif
        }
        private void SmokeLog(string text, string trace, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) smokeErrors.Add(text); }
        private void SmokeCheck(bool pass, string description)
        {
            if (!pass) throw new Exception(description);
            smokeResults.Add("PASS " + description);
        }
        private IEnumerator SmokeCapture(bool corrupted)
        {
            WildCreature target = World.wild.First(w => !w.captured);
            World.controller.enabled = false;
            World.tamer.transform.position = target.root.transform.position + Vector3.back * 2;
            World.controller.enabled = Role == PlayerRole.Tamer;
            if (corrupted) SmokeCheck(TryCorrupt(target), "Corruption request accepted");
            SmokeCheck(BeginCapture(target), "Capture started in range");
            captureIntervened = true;
            float deadline = Time.realtimeSinceStartup + 7;
            while (ScreenState == CampaignScreen.Capture && Time.realtimeSinceStartup < deadline) yield return null;
            SmokeCheck(ScreenState == (corrupted ? CampaignScreen.ChoosePartner : Run.balls == 0 && Run.catches >= CampaignRun.RequiredCatches ? CampaignScreen.StageClear : CampaignScreen.Explore), "Capture finishes in correct state");
        }
        private IEnumerator SmokeShot(string name)
        {
            yield return null; yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                ScreenCapture.CaptureScreenshot(Path.Combine(smokeFolder, name + ".png"));
            yield return new WaitForSeconds(.25f);
        }
        private IEnumerator SmokeFlow()
        {
            float videoDeadline = Time.realtimeSinceStartup + 12;
            while (ScreenState == CampaignScreen.Intro && video != null && !video.isPrepared && Time.realtimeSinceStartup < videoDeadline) yield return null;
            smokeResults.Add(video != null && video.isPrepared ? "PASS Opening video prepared" : "NOTE Opening decoder unavailable in this execution mode; skip fallback used");
            yield return SmokeShot("01-intro");
            OpenMenu(); yield return SmokeShot("02-title");
            menuStep = 1; yield return SmokeShot("03-level-selection");
            menuStep = 2; yield return SmokeShot("04-character-selection");
            menuStep = 3; yield return SmokeShot("05-role-selection");
            StartCampaign(0, 1, PlayerRole.Tamer, "charmander");
            SmokeCheck(ScreenState == CampaignScreen.Explore && Run.party.Count == 1, "Campaign begins with one starter");
            for (int stage = 0; stage < 3; stage++)
            {
                SmokeCheck(Run.stage == stage && World.MapMeshCount > 0 && World.NavigableNodes >= 20, "Map " + (stage + 1) + " loaded with connected terrain");
                SmokeCheck(Camera.allCamerasCount == 1 && Camera.main == view, "Only the campaign camera renders map " + (stage + 1));
                SmokeCheck(World.wild.Count >= 6 && World.wild.All(w => w.creature.species.types.Contains(Run.Habitat) && w.creature.level >= Run.MinLevel && w.creature.level <= Run.MaxLevel), "Map " + (stage + 1) + " type and level filters");
                SmokeCheck(World.InsideSpawnArea(World.tamer.transform.position) && World.InsideSpawnArea(World.warden.transform.position) && World.wild.All(w=>World.InsideSpawnArea(w.root.transform.position)), "All players and creatures spawn inside the tower/tree arena");
                SmokeCheck(World.wild.Count>=11,"Enough distinct creatures for both objectives");
                yield return SmokeShot("06-map-" + (stage + 1));
                yield return SmokeCapture(false);
                SmokeCheck(Run.catches == 1 && Run.balls == 9, "Clean catch spends exactly one ball");
                if (stage == 0)
                {
                    yield return SmokeCapture(true);
                    ChoosePartner(0);
                    SmokeCheck(ScreenState == CampaignScreen.Battle, "Corrupted capture enters battle");
                    yield return SmokeShot("07-battle");
                    int count = Run.party.Count;
                    opponent.health = 1; tamerTurn = true; turnDelay = 0;
                    PlayMove(0);
                    SmokeCheck(Run.catches == 2 && Run.party.Count == count + 1 && Run.balls == 8, "Battle victory catches without spending a second ball");
                    yield return SmokeCapture(true);
                    ChoosePartner(1);
                    string lostId = Run.party[1].species.id;
                    count = Run.party.Count;
                    for (int retry = 0; retry < 8 && ScreenState == CampaignScreen.Battle; retry++)
                    {
                        Run.party[partyIndex].health = 1; tamerTurn = false; ResolveTurn(0);
                    }
                    SmokeCheck(ScreenState == CampaignScreen.Explore && Run.party.Count == count - 1 && Run.catches == 2, "Losing a wild battle removes only the selected teammate");
                    // The lost encounter remains corrupt. Use a different clean target for the last catch.
                    WildCreature clean = World.wild.First(w => !w.captured && !w.corrupted);
                    World.wild.Remove(clean); World.wild.Insert(0, clean);
                }
                else yield return SmokeCapture(false);
                yield return SmokeCapture(false);
                while (Run.catches < CampaignRun.RequiredCatches) {
                    WildCreature extra = World.wild.First(w => !w.captured && !w.corrupted);
                    World.wild.Remove(extra); World.wild.Insert(0, extra);
                    yield return SmokeCapture(false);
                }
                if(stage==0) {
                    SmokeCheck(ScreenState==CampaignScreen.Explore,"Five catches leave time for optional bonus catches");
                    for(int extraCatch=0;extraCatch<1;extraCatch++) {
                        WildCreature extra=World.wild.First(w=>!w.captured&&!w.corrupted);
                        World.wild.Remove(extra); World.wild.Insert(0,extra); yield return SmokeCapture(false);
                    }
                    SmokeCheck(TamerBonus==1 && Run.catches==6,"Six catches earn exactly one Tamer bonus point");
                }
                FinishCaptureGoal();
                SmokeCheck(ScreenState == CampaignScreen.StageClear && Run.catches >= CampaignRun.RequiredCatches, "Minimum five catches clear map " + (stage + 1));
                yield return SmokeShot("08-clear-" + (stage + 1));
                Advance();
            }
            SmokeCheck(finalBattle && bossTeam.Count == 3 && bossTeam.All(c => c.level == 50) && Run.party.All(c => c.level >= 45 && c.health == c.MaxHealth), "Final duel creates three random opponents and heals teams");
            yield return SmokeShot("09-final-duel");
            for (int i = 0; i < 10 && ScreenState == CampaignScreen.Battle; i++)
            {
                partyIndex = 0; opponent.health = 1; tamerTurn = true; turnDelay = 0; PlayMove(0);
            }
            SmokeCheck(ScreenState == CampaignScreen.Result && outcomeTamer, "Final team defeat completes campaign");
            yield return SmokeShot("10-victory");
            StartCampaign(0, 0, PlayerRole.Tamer, "bulbasaur"); Run.seconds = .01f;
            yield return new WaitForSeconds(.1f);
            SmokeCheck(ScreenState == CampaignScreen.Result && !outcomeTamer, "Runtime timeout awards Corruptor victory");
            StartCampaign(0, 2, PlayerRole.Tamer, "squirtle"); Run.balls = 2; CheckOutcome();
            SmokeCheck(ScreenState == CampaignScreen.Result, "Insufficient balls ends run");
            StartCampaign(0, 3, PlayerRole.Corruptor, "bulbasaur");
            for(int i=0;i<CampaignRun.RequiredCorruptions;i++) {
                var target=World.wild[i]; World.wardenController.enabled=false;
                World.warden.transform.position=target.root.transform.position+Vector3.right*2;
                World.wardenController.enabled=true; corruptionEnergy=100; wardenCorruptCooldown=0;
                SmokeCheck(TryCorrupt(target),"Distinct corruption accepted " + (i+1));
                wardenCorruptCooldown=0;
                SmokeCheck(!TryCorrupt(target) && CorruptionCount==i+1,"Duplicate corruption cannot increase objective");
                if(i<CampaignRun.RequiredCorruptions-1) SmokeCheck(ScreenState==CampaignScreen.Explore,"No early corruption victory");
            }
            SmokeCheck(ScreenState==CampaignScreen.Explore && WardenBonus==1,"Six corruptions award a bonus without ending the match");
            StartCampaign(0, 3, PlayerRole.Corruptor, "bulbasaur");
            yield return SmokeShot("11-corruptor-view");
            float aiDeadline = Time.realtimeSinceStartup + 40;
            while (ScreenState == CampaignScreen.Explore && Time.realtimeSinceStartup < aiDeadline) yield return null;
            SmokeCheck(ScreenState == CampaignScreen.Capture, "AI Tamer navigates to a capture target / "+ScreenState+" / route "+aiRoute.Count);
            World.wardenController.enabled=false; World.warden.transform.position=encounter.root.transform.position+Vector3.right*3; World.wardenController.enabled=true;
            SmokeCheck(TryCorrupt(encounter), "Human Corruptor interrupts AI capture");
            while (ScreenState != CampaignScreen.Battle && Time.realtimeSinceStartup < aiDeadline) yield return null;
            SmokeCheck(ScreenState == CampaignScreen.Battle, "Corruptor role reaches battle against AI Tamer");
            for (int retry = 0; retry < 8 && ScreenState == CampaignScreen.Battle; retry++)
            {
                Run.party[0].health = 1; tamerTurn = false; turnDelay = 0; PlayMove(0);
            }
            SmokeCheck(ScreenState == CampaignScreen.Result && Run.party.Count == 0 && !outcomeTamer, "Last teammate loss awards Corruptor victory");
            yield return SmokeShot("12-corruptor-win");
        }
    }
}
#endif
