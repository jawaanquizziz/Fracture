using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Valmon.Editor
{
    [InitializeOnLoad]
    public static class ModelShowcaseBuilder
    {
        private static readonly string[] ValorantPaths = new string[]
        {
            "Assets/Models/Valorant/Clove.glb",
            "Assets/Models/Valorant/Gekko.glb",
            "Assets/Models/Valorant/Reyna.glb",
            "Assets/Models/Valorant/Tejo.glb"
        };

        private static readonly string[] PokemonPaths = new string[]
        {
            "Assets/Models/Pokemon/Bulbasaur.glb",
            "Assets/Models/Pokemon/Ivysaur.glb",
            "Assets/Models/Pokemon/Venusaur.glb",
            "Assets/Models/Pokemon/Charizard.glb",
            "Assets/Models/Pokemon/PokeBall.glb"
        };

        static ModelShowcaseBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Assets/Scenes/ModelShowcase.unity"))
                {
                    Debug.Log("<color=#55ff99><b>[Valmon]</b> Generating initial ModelShowcase scene...</color>");
                    BuildShowcase(true);
                }

                if (!SessionState.GetBool("ValmonShowcaseNotified", false))
                {
                    SessionState.SetBool("ValmonShowcaseNotified", true);
                    Debug.Log("<color=#55ff99><b>[Valmon]</b> Models organized! Use <b>Tools > 🌟 Showcase All Models in Scene</b> to display all 9 models!</color>");
                }
            };
        }

        [MenuItem("Tools/🌟 Showcase All Models in Scene", false, 1)]
        public static void ShowcaseInCurrentScene()
        {
            BuildShowcase(false);
        }

        [MenuItem("Tools/🎪 Create & Open Showcase Scene", false, 2)]
        public static void CreateNewShowcaseScene()
        {
            BuildShowcase(true);
        }

        public static void BuildShowcase(bool createNewScene)
        {
            if (createNewScene)
            {
                Scene newScene = EditorSceneManager.NewScene(NewSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            // Remove existing showcase root if present
            GameObject existingRoot = GameObject.Find("--- MODEL SHOWCASE ---");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            GameObject showcaseRoot = new GameObject("--- MODEL SHOWCASE ---");
            Undo.RegisterCreatedObjectUndo(showcaseRoot, "Create Model Showcase");

            // 1. Create Showcase Stage / Platform
            CreateStage(showcaseRoot.transform);

            // 2. Instantiate Valorant Agents (Back Row)
            float valorantSpacing = 2.4f;
            float valorantStartX = -((ValorantPaths.Length - 1) * valorantSpacing) / 2f;
            float valorantZ = 2.2f;

            for (int i = 0; i < ValorantPaths.Length; i++)
            {
                string path = ValorantPaths[i];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Valmon] Could not load model at: {path}");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, showcaseRoot.transform);
                if (instance == null) instance = Object.Instantiate(prefab, showcaseRoot.transform);

                instance.name = Path.GetFileNameWithoutExtension(path) + " (Valorant)";
                Vector3 targetPos = new Vector3(valorantStartX + i * valorantSpacing, 0f, valorantZ);
                NormalizeAndGround(instance, targetHeight: 1.8f, targetPos);
            }

            // 3. Instantiate Pokemon Models (Front Row)
            float pokemonSpacing = 2.0f;
            float pokemonStartX = -((PokemonPaths.Length - 1) * pokemonSpacing) / 2f;
            float pokemonZ = -0.5f;

            float[] targetHeights = new float[] { 0.7f, 1.0f, 1.6f, 2.2f, 0.45f };

            for (int i = 0; i < PokemonPaths.Length; i++)
            {
                string path = PokemonPaths[i];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Valmon] Could not load model at: {path}");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, showcaseRoot.transform);
                if (instance == null) instance = Object.Instantiate(prefab, showcaseRoot.transform);

                instance.name = Path.GetFileNameWithoutExtension(path) + " (Pokemon)";
                Vector3 targetPos = new Vector3(pokemonStartX + i * pokemonSpacing, 0f, pokemonZ);
                float h = (i < targetHeights.Length) ? targetHeights[i] : 1.0f;
                NormalizeAndGround(instance, targetHeight: h, targetPos);
            }

            // 4. Configure Camera
            SetupCamera(showcaseRoot.transform);

            // 5. Setup Lighting
            SetupLighting(showcaseRoot.transform);

            // Mark scene dirty so changes persist
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            if (createNewScene)
            {
                string scenePath = "Assets/Scenes/ModelShowcase.unity";
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
                Debug.Log($"<color=#55ff99><b>[Valmon]</b> Saved showcase scene to <b>{scenePath}</b>!</color>");
            }

            // Frame scene view camera
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0, 1.2f, 0.8f), new Vector3(12, 4, 8)), false);
            }

            Debug.Log("<color=#55ff99><b>[Valmon]</b> 🌟 Showcase successfully built with all 9 models!</color>");
        }

        private static void CreateStage(Transform parent)
        {
            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = "Showcase_Floor";
            stage.transform.SetParent(parent);
            stage.transform.position = new Vector3(0, -0.1f, 0.8f);
            stage.transform.localScale = new Vector3(14f, 0.2f, 8f);

            // Apply a nice dark material
            Material stageMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            stageMat.color = new Color(0.12f, 0.13f, 0.16f);
            if (stageMat.HasProperty("_Smoothness")) stageMat.SetFloat("_Smoothness", 0.65f);
            if (stageMat.HasProperty("_Metallic")) stageMat.SetFloat("_Metallic", 0.35f);

            stage.GetComponent<Renderer>().sharedMaterial = stageMat;
        }

        private static void NormalizeAndGround(GameObject go, float targetHeight, Vector3 targetPosition)
        {
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                go.transform.position = targetPosition;
                return;
            }

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            float currentHeight = combinedBounds.size.y;
            if (currentHeight > 0.001f && Mathf.Abs(currentHeight - targetHeight) > 0.05f)
            {
                float scaleFactor = targetHeight / currentHeight;
                go.transform.localScale = Vector3.one * scaleFactor;
            }

            // Recalculate bounds after scale
            combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Offset so model rests on the floor at Y = 0
            float yOffset = -combinedBounds.min.y;
            go.transform.position = targetPosition + new Vector3(0, yOffset, 0);

            // Add subtle rotation facing slightly inward or forward
            float rotY = -targetPosition.x * 2.5f; // Slight curve toward center
            go.transform.rotation = Quaternion.Euler(0, 180f + rotY, 0); // Faces camera
        }

        private static void SetupCamera(Transform root)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0, 2.2f, -6.5f);
            cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

            // Attach ShowcaseViewer for turntable/orbit controls
            ShowcaseViewer viewer = cam.GetComponent<ShowcaseViewer>();
            if (viewer == null) viewer = cam.gameObject.AddComponent<ShowcaseViewer>();

            viewer.distance = 7.5f;
            viewer.targetOffset = new Vector3(0, 1.1f, 0.8f);
            viewer.autoRotate = false; // Start static, toggleable with Space
        }

        private static void SetupLighting(Transform root)
        {
            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                dirLight.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
                dirLight.intensity = 1.8f;
                dirLight.color = new Color(1f, 0.98f, 0.95f);
            }

            // Add soft fill light if none exists
            GameObject fillLightObj = GameObject.Find("Showcase_FillLight");
            if (fillLightObj == null)
            {
                fillLightObj = new GameObject("Showcase_FillLight");
                fillLightObj.transform.SetParent(root);
                fillLightObj.transform.rotation = Quaternion.Euler(25f, 140f, 0f);
                Light fillLight = fillLightObj.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.intensity = 0.6f;
                fillLight.color = new Color(0.7f, 0.8f, 1f);
            }
        }
    }
}
