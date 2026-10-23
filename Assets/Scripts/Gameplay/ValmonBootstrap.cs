using UnityEngine;
using Valmon.Environment;
using Valmon.UI;
using Valmon.Network;

namespace Valmon.Gameplay
{
    public static class ValmonBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureGameSetup()
        {
            if (Object.FindAnyObjectByType<GameManager>() != null)
            {
                // GameManager already exists in the scene
                return;
            }

            Debug.Log("<color=#55ff99><b>[Valmon]</b> Auto-bootstrapping playable 3D Game environment...</color>");

            // 1. Directional Light
            if (Object.FindAnyObjectByType<Light>() == null)
            {
                GameObject sunObj = new GameObject("Directional Light");
                Light sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.96f, 0.88f);
                sun.intensity = 1.6f;
                sun.shadows = LightShadows.Soft;
                sunObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.4f, 0.5f, 0.65f);
                RenderSettings.ambientEquatorColor = new Color(0.25f, 0.3f, 0.35f);
            }

            // 2. Arena Manager (Loads real Map1.fbx / Map2.fbx / Map3.fbx and spawns creatures)
            GameObject arenaObj = new GameObject("--- ARENA MANAGER ---");
            ArenaManager arenaMgr = arenaObj.AddComponent<ArenaManager>();
            arenaMgr.mapPrefabs = new GameObject[]
            {
                Resources.Load<GameObject>("Maps/Map1"),
                Resources.Load<GameObject>("Maps/Map2"),
                Resources.Load<GameObject>("Maps/Map3")
            };
            arenaMgr.creaturePrefabs = new GameObject[]
            {
                Resources.Load<GameObject>("Characters/bulbasaur"),
                Resources.Load<GameObject>("Characters/charizard"),
                Resources.Load<GameObject>("Characters/ivysaur"),
                Resources.Load<GameObject>("Characters/Squirtle")
            };

            // 3. Player Character (Tamer)
            GameObject playerObj = new GameObject("Player_Tamer");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0, 1.5f, -6f);

            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.95f, 0);

            PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();

            // Visual Model (Clove / Reyna Valorant agent)
            GameObject playerModelPrefab = Resources.Load<GameObject>("Characters/clove__valorant")
                                        ?? Resources.Load<GameObject>("Characters/reyna__valorant");
            if (playerModelPrefab != null)
            {
                GameObject visual = Object.Instantiate(playerModelPrefab, playerObj.transform);
                visual.name = "ValorantAgent_Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                playerCtrl.visualRoot = visual.transform;
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(playerObj.transform);
                capsule.transform.localPosition = new Vector3(0, 0.95f, 0);
                capsule.transform.localScale = new Vector3(0.8f, 0.95f, 0.8f);
                Object.Destroy(capsule.GetComponent<Collider>());
                playerCtrl.visualRoot = capsule.transform;
            }

            // Capture System (Poké Ball thrower)
            CaptureSystem captureSys = playerObj.AddComponent<CaptureSystem>();
            captureSys.pokeBallPrefab = Resources.Load<GameObject>("Characters/poke_ball");

            // 4. Third-Person Camera
            Camera existingCam = Camera.main;
            GameObject tamerCamObj = (existingCam != null) ? existingCam.gameObject : new GameObject("Main Camera");
            tamerCamObj.name = "Tamer_Camera";
            tamerCamObj.tag = "MainCamera";
            if (tamerCamObj.GetComponent<Camera>() == null) tamerCamObj.AddComponent<Camera>();
            if (tamerCamObj.GetComponent<AudioListener>() == null) tamerCamObj.AddComponent<AudioListener>();

            ThirdPersonCamera tpCam = tamerCamObj.GetComponent<ThirdPersonCamera>();
            if (tpCam == null) tpCam = tamerCamObj.AddComponent<ThirdPersonCamera>();
            tpCam.target = playerObj.transform;
            tpCam.distance = 4.5f;
            tpCam.followOffset = new Vector3(0, 1.6f, 0);
            playerCtrl.cameraTransform = tamerCamObj.transform;

            // 5. Warden Drone / Overhead Camera
            GameObject wardenCamObj = new GameObject("Warden_Camera");
            wardenCamObj.transform.position = new Vector3(0, 24f, 0);
            wardenCamObj.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            Camera wardenCam = wardenCamObj.AddComponent<Camera>();
            wardenCamObj.AddComponent<WardenController>();
            wardenCamObj.SetActive(false);

            // 6. Game Manager & HUD & Networking
            GameObject gameManagerObj = new GameObject("--- GAME MANAGER ---");
            GameManager gameMgr = gameManagerObj.AddComponent<GameManager>();
            gameMgr.tamerPlayer = playerObj;
            gameMgr.tamerCameraRig = tamerCamObj;
            gameMgr.wardenCameraRig = wardenCamObj;
            gameMgr.roundDuration = 75f;

            gameManagerObj.AddComponent<GameHUD>();
            gameManagerObj.AddComponent<ValmonNetworkManager>();

            Debug.Log("<color=#55ff99><b>[Valmon]</b> Ready to Play! Control with WASD, throw Poké Balls with [E] / [Right-Click], switch roles with [Tab].</color>");
        }
    }
}
