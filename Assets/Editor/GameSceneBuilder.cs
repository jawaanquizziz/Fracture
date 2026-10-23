using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Valmon.Gameplay;
using Valmon.Environment;
using Valmon.UI;

namespace Valmon.Editor
{
    [InitializeOnLoad]
    public static class GameSceneBuilder
    {
        private const string GameScenePath = "Assets/Scenes/MainGame.unity";

        static GameSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(GameScenePath))
                {
                    Debug.Log("<color=#55ff99><b>[Valmon]</b> Generating playable game scene at " + GameScenePath + "...</color>");
                    BuildPlayableGameScene();
                }
            };
        }

        [MenuItem("Tools/🎮 Build & Open Playable Game Scene", false, 0)]
        public static void BuildPlayableGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildGameObjectsInScene();
            EditorSceneManager.SaveScene(scene, GameScenePath);
            SetSceneAsBuildIndex0(GameScenePath);
            Debug.Log($"<color=#55ff99><b>[Valmon]</b> 🎮 Playable Game Scene successfully built at <b>{GameScenePath}</b>!</color>");
        }

        [MenuItem("Tools/⚡ Setup Game In Current Scene", false, 1)]
        public static void SetupInCurrentScene()
        {
            BuildGameObjectsInScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("<color=#55ff99><b>[Valmon]</b> ⚡ Game setup successfully injected into current scene!</color>");
        }

        public static void BuildGameObjectsInScene()
        {

            // 2. Setup Lighting & Sky
            SetupLighting();

            // 3. Load Map Assets
            GameObject map1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Maps/Map1.fbx");
            GameObject map2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Maps/Map2.fbx");
            GameObject map3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Maps/Map3.fbx");

            // 4. Load Character & Creature Assets
            GameObject playerVisualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/clove__valorant.glb")
                                         ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Valorant/Clove.glb")
                                         ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/reyna__valorant.glb");

            GameObject pokeBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/poke_ball.glb")
                                     ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Pokemon/PokeBall.glb");

            GameObject bulbasaur = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/bulbasaur.glb")
                                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Pokemon/Bulbasaur.glb");

            GameObject charizard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/charizard.glb")
                                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Pokemon/Charizard.glb");

            GameObject ivysaur = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/ivysaur.glb")
                              ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Pokemon/Ivysaur.glb");

            GameObject squirtle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/squirtle/Squirtle.fbx");

            // 5. Setup ArenaManager
            GameObject arenaManagerObj = new GameObject("--- ARENA MANAGER ---");
            ArenaManager arenaMgr = arenaManagerObj.AddComponent<ArenaManager>();
            arenaMgr.mapPrefabs = new GameObject[] { map1, map2, map3 };
            arenaMgr.creaturePrefabs = new GameObject[] { bulbasaur, charizard, ivysaur, squirtle };
            arenaMgr.playerSpawnPos = new Vector3(0, 1.5f, -8f);

            // 6. Setup Player Character
            GameObject playerObj = new GameObject("Player_Tamer");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0, 1.5f, -8f);

            CharacterController cc = playerObj.AddComponent<CharacterController>();
            cc.radius = 0.45f;
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);

            PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();

            CaptureSystem captureSystem = playerObj.AddComponent<CaptureSystem>();
            captureSystem.pokeBallPrefab = pokeBallPrefab;

            // Visual model attachment
            if (playerVisualPrefab != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(playerVisualPrefab, playerObj.transform);
                if (visual == null) visual = Object.Instantiate(playerVisualPrefab, playerObj.transform);

                visual.name = "Character_Visual";
                NormalizeModelVisual(visual, targetHeight: 1.8f);
                playerCtrl.visualRoot = visual.transform;
            }

            // 7. Setup Tamer Third-Person Camera
            GameObject tamerCamObj = new GameObject("Tamer_Camera");
            tamerCamObj.tag = "MainCamera";
            Camera tamerCam = tamerCamObj.AddComponent<Camera>();
            tamerCamObj.AddComponent<AudioListener>();

            ThirdPersonCamera tpCam = tamerCamObj.AddComponent<ThirdPersonCamera>();
            tpCam.target = playerObj.transform;
            tpCam.distance = 4.5f;
            tpCam.followOffset = new Vector3(0, 1.6f, 0);
            playerCtrl.cameraTransform = tamerCamObj.transform;

            // 8. Setup Warden Drone / Overhead Camera
            GameObject wardenCamObj = new GameObject("Warden_Camera");
            wardenCamObj.transform.position = new Vector3(0, 24f, 0);
            wardenCamObj.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            Camera wardenCam = wardenCamObj.AddComponent<Camera>();
            wardenCamObj.AddComponent<WardenController>();
            wardenCamObj.SetActive(false); // Starts inactive until switched

            // 9. Setup GameManager & HUD
            GameObject gameManagerObj = new GameObject("--- GAME MANAGER ---");
            GameManager gameMgr = gameManagerObj.AddComponent<GameManager>();
            gameMgr.tamerPlayer = playerObj;
            gameMgr.tamerCameraRig = tamerCamObj;
            gameMgr.wardenCameraRig = wardenCamObj;
            gameMgr.roundDuration = 75f;

            gameManagerObj.AddComponent<GameHUD>();
            gameManagerObj.AddComponent<Valmon.Network.ValmonNetworkManager>();

        }

        private static void SetupLighting()
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
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.14f, 0.16f);
        }

        private static void NormalizeModelVisual(GameObject visual, float targetHeight)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float currentH = bounds.size.y;
            if (currentH > 0.01f && Mathf.Abs(currentH - targetHeight) > 0.05f)
            {
                visual.transform.localScale = Vector3.one * (targetHeight / currentH);
            }

            // Ground offset
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            visual.transform.localPosition = new Vector3(0, -bounds.min.y + visual.transform.position.y, 0);
        }

        private static void SetSceneAsBuildIndex0(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Remove existing if found
            scenes.RemoveAll(s => s.path == scenePath);

            // Insert at index 0
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
