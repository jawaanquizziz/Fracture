using System.Collections.Generic;
using UnityEngine;
using Valmon.Gameplay;

namespace Valmon.Environment
{
    public class ArenaManager : MonoBehaviour
    {
        [Header("Map Prefabs / Assets")]
        public GameObject[] mapPrefabs;
        public int activeMapIndex = 0;

        [Header("Spawn Points")]
        public Vector3 playerSpawnPos = new Vector3(0, 1.5f, -6f);
        public Vector3[] creatureSpawnOffsets = new[]
        {
            new Vector3(-4f, 1f, 6f),
            new Vector3(6f, 1f, 8f),
            new Vector3(-8f, 1f, -2f),
            new Vector3(4f, 1f, -4f)
        };

        [Header("Creature Models")]
        public GameObject[] creaturePrefabs;

        private GameObject currentMapInstance;
        private List<GameObject> activeCreatures = new List<GameObject>();

        void Start()
        {
            LoadArena(activeMapIndex);
        }

        public void LoadArena(int index)
        {
            activeMapIndex = index;

            // Clear previous map
            if (currentMapInstance != null)
            {
                Destroy(currentMapInstance);
            }

            // Clear previous creatures
            foreach (var c in activeCreatures)
            {
                if (c != null) Destroy(c);
            }
            activeCreatures.Clear();

            // 1. Instantiate Map
            GameObject mapAsset = (mapPrefabs != null && index < mapPrefabs.Length) ? mapPrefabs[index] : null;
            if (mapAsset == null)
            {
                mapAsset = Resources.Load<GameObject>($"Maps/Map{index + 1}");
            }

            if (mapAsset != null)
            {
                currentMapInstance = Instantiate(mapAsset, Vector3.zero, Quaternion.identity);
                currentMapInstance.name = $"Active_Arena_Map{index + 1}";
                SetupMapColliders(currentMapInstance);
            }
            else
            {
                // Fallback procedural ground if FBX missing
                currentMapInstance = CreateFallbackArena(index);
            }

            // 2. Position Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = playerSpawnPos;
                if (cc != null) cc.enabled = true;
            }

            // 3. Spawn Creatures
            SpawnArenaCreatures();
        }

        private void SetupMapColliders(GameObject mapObj)
        {
            // Ensure all meshes have MeshColliders for collision
            MeshFilter[] meshFilters = mapObj.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    // Only convex if not static or dynamic
                    mc.convex = false;
                }
            }
        }

        private GameObject CreateFallbackArena(int index)
        {
            GameObject fallbackRoot = new GameObject($"Fallback_Arena_Map{index + 1}");

            // Ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(fallbackRoot.transform);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(60f, 1f, 60f);

            Color groundColor = (index == 0) ? new Color(0.2f, 0.35f, 0.2f) : // Forest
                                (index == 1) ? new Color(0.35f, 0.3f, 0.25f) : // Fortress
                                               new Color(0.25f, 0.35f, 0.45f); // Coastal

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = groundColor;
            ground.GetComponent<Renderer>().sharedMaterial = mat;

            // Boundary Walls
            CreateBoundaryWall(fallbackRoot.transform, new Vector3(0, 3f, 30f), new Vector3(60f, 6f, 1f));
            CreateBoundaryWall(fallbackRoot.transform, new Vector3(0, 3f, -30f), new Vector3(60f, 6f, 1f));
            CreateBoundaryWall(fallbackRoot.transform, new Vector3(30f, 3f, 0), new Vector3(1f, 6f, 60f));
            CreateBoundaryWall(fallbackRoot.transform, new Vector3(-30f, 3f, 0), new Vector3(1f, 6f, 60f));

            return fallbackRoot;
        }

        private void CreateBoundaryWall(Transform parent, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().enabled = false; // Invisible collider
        }

        private void SpawnArenaCreatures()
        {
            string[] creatureAssetNames = new[] { "bulbasaur", "charizard", "ivysaur", "Squirtle" };
            string[] displayNames = new[] { "Bulbasaur", "Charizard", "Ivysaur", "Squirtle" };

            for (int i = 0; i < creatureSpawnOffsets.Length; i++)
            {
                Vector3 spawnPos = creatureSpawnOffsets[i];
                GameObject prefab = (creaturePrefabs != null && i < creaturePrefabs.Length) ? creaturePrefabs[i] : null;

                if (prefab == null && i < creatureAssetNames.Length)
                {
                    prefab = Resources.Load<GameObject>($"Characters/{creatureAssetNames[i]}");
                }

                GameObject creatureObj;
                if (prefab != null)
                {
                    creatureObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0));
                }
                else
                {
                    // Fallback creature
                    creatureObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    creatureObj.name = $"Creature_{i + 1}";
                    creatureObj.transform.localScale = Vector3.one * 0.8f;
                    creatureObj.transform.position = spawnPos;
                }

                CreatureController cc = creatureObj.GetComponent<CreatureController>();
                if (cc == null) cc = creatureObj.AddComponent<CreatureController>();

                // Configure creature names
                if (i < displayNames.Length) cc.creatureName = displayNames[i];

                activeCreatures.Add(creatureObj);
            }
        }
    }
}
