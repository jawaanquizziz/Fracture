using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Valmon.Campaign
{
    public sealed class WildCreature
    {
        public OwnedCreature creature;
        public GameObject root;
        public bool corrupted, captured;
        public int node;
        public LineRenderer ring;
        public Transform visual;
        public Vector3 visualBasePos;
        public float visualHeight;
        public Transform marker;
        public Renderer markerRenderer;
        public Vector3 wanderTarget;
        public float wanderWaitTimer;
        public float moveSpeed = 1.1f;
    }

    public sealed partial class CampaignWorld : IDisposable
    {
        public const int TerrainLayer = 8, CreatureLayer = 9;
        public const float PlayerHeight = 1.15f;
        public const float CreatureHeight = 0.75f;

        // Per-stage visual + spawn tuning so each map feels proportioned.
        public struct StageProfile
        {
            public float TowerScale;     // uniform scale on the Corruptor spire
            public float CreatureScale;  // multiplier applied to each wild creature's visible size
            public float SpawnRadius;    // creatures only spawn inside this radius of (0,0); 0 = unbounded
        }
        // All maps now share the small Map 1 spire size; creatures and radii still vary.
        public static StageProfile ProfileFor(int stage) => stage == 0
            ? new StageProfile { TowerScale = .22f, CreatureScale = .9f,  SpawnRadius = 11f }   // Grass: tiny spire, tight ring
            : stage == 1
            ? new StageProfile { TowerScale = .22f, CreatureScale = 1.0f, SpawnRadius = 16f }   // Fire
            :                    new StageProfile { TowerScale = .22f, CreatureScale = 1.15f, SpawnRadius = 11f };  // Water
        private const int Grid = 81;
        public Vector3 ArenaCenter { get; private set; }
        public float ArenaRadius { get; private set; }
        public bool InsideSpawnArea(Vector3 p) { Vector2 d=new Vector2(p.x-ArenaCenter.x,p.z-ArenaCenter.z); return d.magnitude>=3.4f && d.magnitude<=ArenaRadius; }
        private readonly CampaignCatalog catalog;
        private readonly List<Material> materials = new List<Material>();
        private GameObject terrainRoot, presentationRoot;
        private readonly Dictionary<int, Vector3> nodes = new Dictionary<int, Vector3>();
        private readonly HashSet<int> reachable = new HashSet<int>();
        public readonly List<WildCreature> wild = new List<WildCreature>();
        public GameObject tamer;
        public CharacterController controller;
        public Vector3 spawn;
        public Vector3 towerPosition;
        public Camera camera;
        public Transform battleTamer, battleEnemy;
        public Vector3 battleTamerHome, battleEnemyHome;
        private readonly List<Transform> skyDrifters = new List<Transform>();
        private Transform towerBeacon;
        private float ambientClock;
        private readonly List<Renderer> waterRenderers = new List<Renderer>();
        private readonly List<Renderer> lavaRenderers = new List<Renderer>();
        private readonly List<Renderer> grassRenderers = new List<Renderer>();
        public int NavigableNodes => reachable.Count;
        public int MapMeshCount { get; private set; }
        public CampaignWorld(CampaignCatalog data, Camera view) { catalog = data; camera = view; }
        public Color Theme(int stage) => stage == 0 ? new Color(.17f, .48f, .32f) : stage == 1 ? new Color(.42f, .20f, .15f) : new Color(.13f, .32f, .48f);

        private Material Material(Color color)
        {
            var material = new Material(catalog.surfaceShader);
            material.color = color;
            materials.Add(material);
            return material;
        }

        public GameObject Model(GameObject prefab, Transform parent, Vector3 position, float size, bool fitHeight = false)
        {
            GameObject pivot = new GameObject(prefab != null ? prefab.name : "Missing model");
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = position;
            if (prefab == null) return pivot;
            GameObject visual = Object.Instantiate(prefab, pivot.transform);
            DisableImportedSceneComponents(visual);
            visual.transform.localPosition = Vector3.zero;
            foreach (Animator animation in visual.GetComponentsInChildren<Animator>(true)) animation.enabled = false;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return pivot;
            Bounds bounds = BoundsOf(renderers);
            float dimension = fitHeight ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            visual.transform.localScale *= size / Mathf.Max(.001f, dimension);
            bounds = BoundsOf(renderers);
            visual.transform.position += pivot.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            FixMaterials(renderers);
            SpeciesDefinition species = catalog.species.FirstOrDefault(s => s.model == prefab && s.albedoOverride != null);
            if (species != null)
            {
                Material textured = Material(Color.white);
                textured.SetTexture("_BaseMap", species.albedoOverride);
                textured.SetFloat("_Smoothness", .25f);
                foreach (Renderer renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => textured).ToArray();
            }
            return pivot;
        }

        private static void DisableImportedSceneComponents(GameObject root)
        {
            foreach (Camera imported in root.GetComponentsInChildren<Camera>(true)) imported.enabled = false;
            foreach (Light imported in root.GetComponentsInChildren<Light>(true)) imported.enabled = false;
            foreach (AudioListener imported in root.GetComponentsInChildren<AudioListener>(true)) imported.enabled = false;
        }

        private void FixMaterials(Renderer[] renderers)
        {
            var replacements = new Dictionary<Material, Material>();
            foreach (Renderer renderer in renderers)
            {
                Material[] assigned = renderer.sharedMaterials;
                for (int i = 0; i < assigned.Length; i++)
                {
                    Material original = assigned[i];
                    if (original == null) { assigned[i] = Material(Color.gray); continue; }
                    if (original.shader != null && (original.shader.name.Contains("Universal") || original.shader.name.Contains("glTF"))) continue;
                    if (!replacements.TryGetValue(original, out Material replacement))
                    {
                        replacement = Material(original.HasProperty("_Color") ? original.color : Color.white);
                        Texture texture = original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
                        if (texture != null) replacement.SetTexture("_BaseMap", texture);
                        replacements.Add(original, replacement);
                    }
                    assigned[i] = replacement;
                }
                renderer.sharedMaterials = assigned;
            }
        }

        public static Bounds BoundsOf(Renderer[] renderers)
        {
            Bounds value = renderers[0].bounds;
            foreach (Renderer renderer in renderers) value.Encapsulate(renderer.bounds);
            return value;
        }

        private GameObject Platform(string name, Transform parent, Vector3 position, Vector3 size, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            obj.layer = TerrainLayer;
            return obj;
        }

        public void ShowMenu() { ShowMapMenu(0); }

        public void LoadStage(CampaignRun run, int character, System.Random random)
        {
            Clear();
            terrainRoot = new GameObject("Playable map " + (run.stage + 1));
            GameObject map = Object.Instantiate(catalog.maps[run.stage], terrainRoot.transform);
            DisableImportedSceneComponents(map);
            map.name = "Original Map " + (run.stage + 1);
            Bounds bounds = BoundsOf(map.GetComponentsInChildren<Renderer>());
            map.transform.localScale *= 80f / Mathf.Max(bounds.size.x, bounds.size.z, 1);
            bounds = BoundsOf(map.GetComponentsInChildren<Renderer>());
            map.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            FixMaterials(map.GetComponentsInChildren<Renderer>());
            foreach (Collider old in map.GetComponentsInChildren<Collider>()) old.enabled = false;
            MapMeshCount = 0;
            foreach (MeshFilter mesh in map.GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.sharedMesh == null) continue;
                if (!mesh.sharedMesh.isReadable) throw new InvalidOperationException("Map mesh is not readable: " + mesh.sharedMesh.name + ". Rebuild with Tools > VALMON > Build Campaign.");
                mesh.gameObject.layer = TerrainLayer;
                MapMeshCount++;
                string meshName = (mesh.name + " " + mesh.sharedMesh.name).ToLowerInvariant();
                // Decorative grass is walk-through. Use trunks, not canopies, for tree collision.
                if (meshName.Contains("grass") || meshName.Contains("flower") || meshName.Contains("leaf") || meshName.Contains("bush")) continue;
                if (meshName.Contains("tree"))
                {
                    Bounds tree = mesh.sharedMesh.bounds;
                    int axis = tree.size.y >= tree.size.x && tree.size.y >= tree.size.z ? 1 : tree.size.z >= tree.size.x ? 2 : 0;
                    CapsuleCollider trunk = mesh.gameObject.AddComponent<CapsuleCollider>();
                    trunk.direction = axis; trunk.height = tree.size[axis] * .65f;
                    trunk.radius = Mathf.Max(.02f, Mathf.Min(tree.size[(axis + 1) % 3], tree.size[(axis + 2) % 3]) * .09f);
                    Vector3 axisVector = Vector3.zero; axisVector[axis] = 1;
                    Vector3 center = tree.center;
                    center[axis] = mesh.transform.TransformDirection(axisVector).y >= 0 ? tree.min[axis] + trunk.height / 2 : tree.max[axis] - trunk.height / 2;
                    trunk.center = center;
                    continue;
                }
                MeshCollider collider = mesh.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh.sharedMesh;
            }
            // A foundation below the imported map closes holes at its outer boundary.
            Platform("Arena foundation", terrainRoot.transform, new Vector3(0, -.3f, 0), new Vector3(92, .4f, 92), Theme(run.stage));
            var towerRenderers = map.GetComponentsInChildren<Renderer>().Where(r => r.name.ToLowerInvariant().Contains("tower")).ToArray();
            ArenaCenter = towerRenderers.Length > 0 ? BoundsOf(towerRenderers).center : Vector3.zero;
            ArenaRadius = ProfileFor(run.stage).SpawnRadius;
            if(towerRenderers.Length > 0) towerPosition = new Vector3(ArenaCenter.x, BoundsOf(towerRenderers).max.y + 2, ArenaCenter.z);
            else BuildTower(run.stage);
            Physics.SyncTransforms();
            bounds = BoundsOf(map.GetComponentsInChildren<Renderer>());
            BuildNavigation(bounds.max.y + 10);
            if (reachable.Count < 20) throw new InvalidOperationException("Map has insufficient connected walking surface.");
            var spawnNodes = reachable.Where(n => InsideSpawnArea(nodes[n]) && SpawnClear(nodes[n])).ToArray();
            if(spawnNodes.Length < 12) throw new InvalidOperationException("Insufficient safe interior spawn points: " + spawnNodes.Length);
            int startNode = spawnNodes.OrderByDescending(n => SpawnScore(nodes[n])).First();
            spawn = nodes[startNode] + Vector3.up * .12f;
            tamer = new GameObject("Tamer");
            tamer.transform.SetParent(terrainRoot.transform);
            tamer.transform.position = spawn;
            Model(catalog.characters[character].model, tamer.transform, Vector3.zero, PlayerHeight, true).transform.localRotation = Quaternion.Euler(0, catalog.characters[character].yawOffset, 0);
            tamer.transform.rotation = Quaternion.Euler(0, StartingYaw, 0);
            tamer.AddComponent<CharacterLocomotion>().Initialize(tamer.transform.GetChild(0));
            controller = tamer.AddComponent<CharacterController>();
            controller.height = PlayerHeight; controller.radius = .22f; controller.center = Vector3.up * (PlayerHeight * .5f);
            controller.stepOffset = .35f; controller.slopeLimit = 50;
            SpeciesDefinition[] candidates = catalog.species.Where(s => s.types.Contains(run.Habitat)).ToArray();
            StageProfile profile = ProfileFor(run.stage);
            var used = new List<Vector3> { spawn };
            var shuffled = spawnNodes.OrderBy(_ => random.Next()).ToList();
            while (shuffled.Count > 0)
            {
                int node=shuffled.OrderByDescending(n=>used.Min(p=>(p-nodes[n]).sqrMagnitude)).First();
                shuffled.Remove(node);
                Vector3 position = nodes[node];
                if (used.Any(p => Vector3.Distance(p, position) < 3.2f)) continue;
                SpeciesDefinition species = candidates[wild.Count % candidates.Length];
                int level = random.Next(run.MinLevel, run.MaxLevel + 1);
                var creature = new WildCreature { creature = new OwnedCreature(species, level), node = node };
                creature.root = Model(species.model, terrainRoot.transform, position + Vector3.up * .06f, CreatureHeight * profile.CreatureScale);
                Bounds creatureBounds = BoundsOf(creature.root.GetComponentsInChildren<Renderer>());
                float enlarge = Mathf.Min(1.4f, (CreatureHeight * profile.CreatureScale * .55f) / Mathf.Max(.01f, creatureBounds.size.y));
                if (enlarge > 1) creature.root.transform.GetChild(0).localScale *= enlarge;
                creatureBounds = BoundsOf(creature.root.GetComponentsInChildren<Renderer>());
                // Re-ground short, wide species after enlargement.
                creature.root.transform.GetChild(0).position += Vector3.up * (position.y + .06f - creatureBounds.min.y);
                creature.visualHeight = creatureBounds.size.y;
                creature.root.name = species.displayName + " Lv " + level;
                creature.root.layer = CreatureLayer;
                SphereCollider hit = creature.root.AddComponent<SphereCollider>();
                hit.center = Vector3.up * (creature.visualHeight * .5f); hit.radius = Mathf.Max(1, creature.visualHeight * .5f); hit.isTrigger = true;
                creature.ring = Ring(creature.root.transform, new Color(.25f, .85f, .65f));
                creature.wanderTarget = position + Vector3.up * .06f;
                creature.wanderWaitTimer = (float)(random.NextDouble() * 2.0);
                creature.moveSpeed = Mathf.Lerp(.9f, 1.4f, (float)random.NextDouble());
                if (creature.root.transform.childCount > 0)
                {
                    creature.visual = creature.root.transform.GetChild(0);
                    creature.visualBasePos = creature.visual.localPosition;
                }
                // Floating orb + vertical light beam so the Corruptor spots creatures from any angle.
                GameObject markerRoot = new GameObject("Marker group");
                markerRoot.transform.SetParent(creature.root.transform, false);
                markerRoot.transform.localPosition = Vector3.zero;
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.GetComponent<Collider>().enabled = false;
                marker.transform.SetParent(markerRoot.transform, false);
                marker.transform.localPosition = new Vector3(0f, Mathf.Max(2.8f, creature.visualHeight + 1.2f), 0f);
                marker.transform.localScale = Vector3.one * .25f;
                Material markerMat = Material(new Color(.4f, 1f, .78f));
                if (markerMat.HasProperty("_EmissionColor")) { markerMat.EnableKeyword("_EMISSION"); markerMat.SetColor("_EmissionColor", new Color(1.3f, 2.1f, 1.5f)); }
                marker.GetComponent<Renderer>().sharedMaterial = markerMat;
                // Column of light under the orb, visible from above.
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beam.name = "Marker beam";
                beam.GetComponent<Collider>().enabled = false;
                beam.transform.SetParent(markerRoot.transform, false);
                beam.transform.localPosition = new Vector3(0f, creature.visualHeight + .35f, 0f);
                beam.transform.localScale = new Vector3(.028f, .3f, .028f);
                Material beamMat = Material(new Color(.4f, 1f, .78f, .55f));
                if (beamMat.HasProperty("_EmissionColor")) { beamMat.EnableKeyword("_EMISSION"); beamMat.SetColor("_EmissionColor", new Color(.9f, 1.7f, 1.1f)); }
                beam.GetComponent<Renderer>().sharedMaterial = beamMat;
                creature.marker = marker.transform;
                creature.markerRenderer = marker.GetComponent<Renderer>();
                wild.Add(creature); used.Add(position);
                if (wild.Count == 12) break;
            }
            if (wild.Count < 11) throw new InvalidOperationException("Need at least eleven separated interior creatures for both objectives; found " + wild.Count);
            camera.fieldOfView = 55;
            // Sky drifters removed — they were bobbing into view and polluting the scene.
            BuildArenaBoundary(run.stage);
            RegisterAnimatedSurfaces(map, run.stage);
            Debug.Log($"[VALMON] Map {run.stage + 1}: {MapMeshCount} meshes, {reachable.Count} reachable nodes, {wild.Count} creatures, {waterRenderers.Count} water, {lavaRenderers.Count} lava, {grassRenderers.Count} grass.");
        }

        private void BuildTower(int stage)
        {
            float s = ProfileFor(stage).TowerScale;
            // Scale the Corruptor's airborne spawn point in step with the spire.
            towerPosition = new Vector3(0f, Mathf.Lerp(22f, 44f, s), 0f);
            GameObject tower = new GameObject("Corruptor spire");
            tower.transform.SetParent(terrainRoot.transform);
            tower.transform.position = Vector3.zero;
            Color spireColor = Color.Lerp(Theme(stage), new Color(.07f, .055f, .14f), .55f);
            Platform("Spire base", tower.transform, new Vector3(0, 3f * s, 0), new Vector3(9f * s, 6f * s, 9f * s), spireColor);
            Platform("Spire mid", tower.transform, new Vector3(0, 11f * s, 0), new Vector3(6.4f * s, 10f * s, 6.4f * s), Color.Lerp(spireColor, Color.black, .25f));
            Platform("Spire upper", tower.transform, new Vector3(0, 22f * s, 0), new Vector3(5f * s, 12f * s, 5f * s), Color.Lerp(spireColor, Color.black, .45f));
            Platform("Spire crown", tower.transform, new Vector3(0, 29.4f * s, 0), new Vector3(10.4f * s, 1.2f * s, 10.4f * s), new Color(.14f, .19f, .28f));
            Platform("Spire finial", tower.transform, new Vector3(0, 32f * s, 0), new Vector3(2.4f * s, 4f * s, 2.4f * s), Color.Lerp(spireColor, Color.black, .65f));
            // Vertical beam of light from base to crown — scaled with the spire.
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Rift beam";
            beam.GetComponent<Collider>().enabled = false;
            beam.transform.SetParent(tower.transform, false);
            beam.transform.localPosition = new Vector3(0, 20f * s, 0);
            beam.transform.localScale = new Vector3(.6f * s, 20f * s, .6f * s);
            Material beamMat = Material(new Color(1f, .4f, 1.6f, .55f));
            if (beamMat.HasProperty("_EmissionColor")) { beamMat.EnableKeyword("_EMISSION"); beamMat.SetColor("_EmissionColor", new Color(1.9f, .55f, 2.1f)); }
            beam.GetComponent<Renderer>().sharedMaterial = beamMat;
            // Beacon atop the finial, also scaled.
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Rift beacon";
            beacon.transform.SetParent(tower.transform, false);
            beacon.transform.localPosition = new Vector3(0, 35f * s, 0);
            beacon.transform.localScale = Vector3.one * (3.2f * s);
            beacon.GetComponent<Collider>().enabled = false;
            beacon.layer = TerrainLayer;
            Material beaconMat = Material(new Color(.9f, .25f, 1f));
            if (beaconMat.HasProperty("_EmissionColor")) { beaconMat.EnableKeyword("_EMISSION"); beaconMat.SetColor("_EmissionColor", new Color(1.8f, .5f, 2f)); }
            beacon.GetComponent<Renderer>().sharedMaterial = beaconMat;
            towerBeacon = beacon.transform;
        }

        private void BuildArenaBoundary(int stage)
        {
            StageProfile profile = ProfileFor(stage);
            if (profile.SpawnRadius <= 0f) return;
            GameObject root = new GameObject("Arena boundary");
            root.transform.SetParent(terrainRoot.transform, false);
            Color postColor = Color.Lerp(Theme(stage), new Color(.95f, .95f, 1f), .35f);
            Material postMat = Material(postColor);
            if (postMat.HasProperty("_EmissionColor")) { postMat.EnableKeyword("_EMISSION"); postMat.SetColor("_EmissionColor", postColor * 1.4f); }
            float r = ArenaRadius + 1.5f;
            int count = 20;
            for (int i = 0; i < count; i++)
            {
                float ang = i * Mathf.PI * 2f / count;
                Vector3 pos = new Vector3(ArenaCenter.x + Mathf.Cos(ang) * r, 0f, ArenaCenter.z + Mathf.Sin(ang) * r);
                // Drop onto the terrain.
                if (Physics.Raycast(pos + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 80f, 1 << TerrainLayer))
                    pos.y = hit.point.y;
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Boundary post " + i;
                post.GetComponent<Collider>().enabled = false;
                post.transform.SetParent(root.transform, false);
                post.transform.position = pos + Vector3.up * 1.1f;
                post.transform.localScale = new Vector3(.18f, 1.1f, .18f);
                post.GetComponent<Renderer>().sharedMaterial = postMat;
            }
        }

        private void BuildSkyDrifters(int stage)
        {
            skyDrifters.Clear();
            GameObject root = new GameObject("Sky drifters");
            root.transform.SetParent(terrainRoot.transform);
            Color tint = Color.Lerp(Theme(stage), Color.white, .55f);
            for (int i = 0; i < 10; i++)
            {
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloud.name = "Drifter " + i;
                cloud.GetComponent<Collider>().enabled = false;
                cloud.transform.SetParent(root.transform, false);
                float angle = i / 10f * Mathf.PI * 2f;
                float radius = 48f + (i % 3) * 6f;
                cloud.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 24f + (i % 4) * 3.5f, Mathf.Sin(angle) * radius);
                cloud.transform.localScale = new Vector3(10f + (i % 3) * 3f, 3.4f, 7f + (i % 4) * 2f);
                cloud.GetComponent<Renderer>().sharedMaterial = Material(new Color(tint.r, tint.g, tint.b, .55f));
                skyDrifters.Add(cloud.transform);
            }
        }

        private void RegisterAnimatedSurfaces(GameObject mapRoot, int stage)
        {
            waterRenderers.Clear(); lavaRenderers.Clear(); grassRenderers.Clear();
            foreach (Renderer renderer in mapRoot.GetComponentsInChildren<Renderer>(true))
            {
                string hay = (renderer.name + " " + renderer.gameObject.name + " " + (renderer.transform.parent != null ? renderer.transform.parent.name : "")).ToLowerInvariant();
                foreach (Material m in renderer.sharedMaterials) if (m != null) hay += " " + m.name.ToLowerInvariant();
                bool water = hay.Contains("water") || hay.Contains("sea") || hay.Contains("ocean") || hay.Contains("river") || hay.Contains("tide");
                bool lava = hay.Contains("lava") || hay.Contains("magma") || hay.Contains("ember");
                bool grass = hay.Contains("grass") || hay.Contains("flower") || hay.Contains("meadow");
                // Stage 2 (coastal/water) biases most of its base to the "water" bucket.
                if (!water && stage == 2 && hay.Contains("sand") == false && renderer.bounds.size.y < 2f && Mathf.Abs(renderer.bounds.center.y) < 1f) water = true;
                if (!(water || lava || grass)) continue;
                // Give this renderer its OWN material so animation doesn't leak globally.
                Material[] shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] == null) continue;
                    Material clone = new Material(shared[i]);
                    materials.Add(clone);
                    shared[i] = clone;
                }
                renderer.sharedMaterials = shared;
                if (water) waterRenderers.Add(renderer);
                else if (lava) lavaRenderers.Add(renderer);
                else grassRenderers.Add(renderer);
            }
        }

        public void TickAmbient(float dt)
        {
            ambientClock += dt;
            // Water scroll + subtle colour shift.
            Vector2 waterOffset = new Vector2(ambientClock * .055f, ambientClock * .022f);
            Color waterTint = Color.Lerp(new Color(.42f, .72f, .95f), new Color(.28f, .55f, .9f), (Mathf.Sin(ambientClock * 1.1f) + 1f) * .5f);
            for (int i = 0; i < waterRenderers.Count; i++)
            {
                Renderer r = waterRenderers[i]; if (r == null) continue;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_BaseMap")) m.SetTextureOffset("_BaseMap", waterOffset);
                    if (m.HasProperty("_MainTex")) m.SetTextureOffset("_MainTex", waterOffset);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", waterTint);
                    else if (m.HasProperty("_Color")) m.SetColor("_Color", waterTint);
                }
            }
            // Lava emission pulse + slow scroll.
            float lavaPulse = (Mathf.Sin(ambientClock * 2.3f) + 1f) * .5f;
            Color lavaBase = Color.Lerp(new Color(1.3f, .35f, .12f), new Color(1.7f, .85f, .22f), lavaPulse);
            Vector2 lavaOffset = new Vector2(ambientClock * .028f, ambientClock * .014f);
            for (int i = 0; i < lavaRenderers.Count; i++)
            {
                Renderer r = lavaRenderers[i]; if (r == null) continue;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_BaseMap")) m.SetTextureOffset("_BaseMap", lavaOffset);
                    if (m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", lavaBase); }
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1f, .45f + lavaPulse * .25f, .18f, 1f));
                }
            }
            // Grass sway — exaggerated UV wiggle + visible brightness breathing.
            Vector2 grassOffset = new Vector2(Mathf.Sin(ambientClock * 2.4f) * .06f, Mathf.Cos(ambientClock * 2.0f) * .045f);
            Color grassTint = Color.Lerp(new Color(.75f, 1.05f, .62f), new Color(.42f, .78f, .35f), (Mathf.Sin(ambientClock * 1.6f) + 1f) * .5f);
            for (int i = 0; i < grassRenderers.Count; i++)
            {
                Renderer r = grassRenderers[i]; if (r == null) continue;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_BaseMap")) m.SetTextureOffset("_BaseMap", grassOffset);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", grassTint);
                    else if (m.HasProperty("_Color")) m.SetColor("_Color", grassTint);
                }
            }
            for (int i = 0; i < skyDrifters.Count; i++)
            {
                Transform t = skyDrifters[i];
                if (t == null) continue;
                float phase = ambientClock * .05f + i * .628f;
                Vector3 p = t.localPosition;
                float radius = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                t.localPosition = new Vector3(Mathf.Cos(phase) * radius, p.y + Mathf.Sin(ambientClock + i) * .003f, Mathf.Sin(phase) * radius);
            }
            if (towerBeacon != null)
            {
                towerBeacon.Rotate(0f, 24f * dt, 0f, Space.Self);
                float s = 1.7f + Mathf.Sin(ambientClock * 2.4f) * .18f;
                towerBeacon.localScale = new Vector3(s, s, s);
            }
            for (int i = 0; i < wild.Count; i++)
            {
                WildCreature c = wild[i];
                if (c == null || c.captured || c.root == null) continue;
                // Wander logic: creatures walk smoothly between safe interior reachable nodes within allowed area
                if (c.wanderWaitTimer > 0f)
                {
                    c.wanderWaitTimer -= dt;
                }
                else
                {
                    Vector3 toTarget = c.wanderTarget - c.root.transform.position; toTarget.y = 0f;
                    float dist = toTarget.magnitude;
                    if (dist > .2f)
                    {
                        Vector3 moveDir = toTarget / dist;
                        float step = Mathf.Min(dist, c.moveSpeed * dt);
                        c.root.transform.position += moveDir * step;
                        // Orient smoothly toward walk direction
                        if (moveDir.sqrMagnitude > .001f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(moveDir);
                            c.root.transform.rotation = Quaternion.Slerp(c.root.transform.rotation, targetRot, dt * 6f);
                        }
                    }
                    else
                    {
                        // Reached current point; pick a neighboring connected node inside allowed spawn area
                        var candidates = Neighbors(c.node).Where(n => reachable.Contains(n) && InsideSpawnArea(nodes[n]) && Connected(c.node, n)).ToList();
                        if (candidates.Count > 0)
                        {
                            int nextNode = candidates[(i + (int)(ambientClock * 10f)) % candidates.Count];
                            c.node = nextNode;
                            c.wanderTarget = nodes[nextNode] + Vector3.up * .06f;
                            c.wanderWaitTimer = UnityEngine.Random.Range(1.8f, 4.5f);
                        }
                        else
                        {
                            c.wanderWaitTimer = 2.0f;
                        }
                    }
                }
                if (c.visual != null)
                {
                    float phase = ambientClock * 2.6f + i * .9f;
                    c.visual.localPosition = c.visualBasePos + new Vector3(0f, Mathf.Abs(Mathf.Sin(phase)) * .09f, 0f);
                }
                if (c.ring != null)
                {
                    float pulse = (c.corrupted ? .17f : .085f) + Mathf.Sin(ambientClock * (c.corrupted ? 6f : 3f) + i) * .03f;
                    c.ring.widthMultiplier = pulse;
                    c.ring.transform.Rotate(0f, (c.corrupted ? 65f : 22f) * dt, 0f, Space.Self);
                }
                if (c.marker != null)
                {
                    float bob = Mathf.Sin(ambientClock * 2.3f + i * 1.3f) * .22f;
                    c.marker.localPosition = new Vector3(0f, c.visualHeight + .7f + bob * .35f, 0f);
                    c.marker.Rotate(0f, 80f * dt, 0f, Space.Self);
                    if (c.markerRenderer != null)
                    {
                        Color baseC = c.corrupted ? new Color(1.1f, .35f, 1.6f) : new Color(.4f, 1f, .78f);
                        float glow = .55f + (Mathf.Sin(ambientClock * (c.corrupted ? 6f : 3.5f) + i) + 1f) * .35f;
                        Material mm = c.markerRenderer.sharedMaterial;
                        if (mm != null)
                        {
                            if (mm.HasProperty("_BaseColor")) mm.SetColor("_BaseColor", baseC);
                            if (mm.HasProperty("_EmissionColor")) mm.SetColor("_EmissionColor", baseC * glow);
                        }
                    }
                }
            }
        }

        private LineRenderer Ring(Transform parent, Color color)
        {
            GameObject root = new GameObject("Capture ring"); root.transform.SetParent(parent, false);
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 40;
            line.widthMultiplier = .065f; line.sharedMaterial = Material(color);
            for (int i = 0; i < 40; i++)
            {
                float angle = i * Mathf.PI * 2 / 40;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), .09f, Mathf.Sin(angle)));
            }
            return line;
        }

        public void Corrupt(WildCreature creature)
        {
            creature.corrupted = true;
            creature.ring.sharedMaterial.color = new Color(.72f, .16f, 1f);
            creature.ring.widthMultiplier = .13f;
        }

        public void SpawnCatchSparkle(Vector3 at)
        {
            GameObject burst = new GameObject("Catch sparkle");
            burst.transform.position = at;
            LineRenderer line = burst.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 32;
            line.widthMultiplier = .22f;
            line.sharedMaterial = Material(new Color(1f, .95f, .55f));
            if (line.sharedMaterial.HasProperty("_EmissionColor")) { line.sharedMaterial.EnableKeyword("_EMISSION"); line.sharedMaterial.SetColor("_EmissionColor", new Color(1.8f, 1.5f, .7f)); }
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * 2.2f, 0f, Mathf.Sin(a) * 2.2f));
            }
            burst.AddComponent<CatchSparkleFade>();
        }

        public sealed class CatchSparkleFade : MonoBehaviour
        {
            private float life = 1.1f;
            private LineRenderer line;
            private void Awake() { line = GetComponent<LineRenderer>(); }
            private void Update()
            {
                life -= Time.deltaTime;
                if (line != null)
                {
                    float t = 1f - Mathf.Clamp01(life / 1.1f);
                    transform.localScale = Vector3.Lerp(Vector3.one * .3f, Vector3.one * 3.2f, t);
                    transform.Rotate(0f, 420f * Time.deltaTime, 0f, Space.Self);
                    line.widthMultiplier = Mathf.Max(0f, .22f * (1f - t));
                }
                if (life <= 0f) Destroy(gameObject);
            }
        }

        public void SpawnRiftBurst(Vector3 center, float radius)
        {
            GameObject burst = new GameObject("Rift burst");
            burst.transform.SetParent(terrainRoot != null ? terrainRoot.transform : null, false);
            burst.transform.position = center + Vector3.up * .1f;
            LineRenderer line = burst.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 56;
            line.widthMultiplier = .35f;
            line.sharedMaterial = Material(new Color(1f, .35f, 1.4f));
            if (line.sharedMaterial.HasProperty("_EmissionColor")) { line.sharedMaterial.EnableKeyword("_EMISSION"); line.sharedMaterial.SetColor("_EmissionColor", new Color(1.6f, .5f, 2f)); }
            for (int i = 0; i < 56; i++)
            {
                float a = i * Mathf.PI * 2 / 56f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .18f, Mathf.Sin(a) * radius));
            }
            burst.AddComponent<RiftBurstFade>();
        }

        // Simple MonoBehaviour hidden here so the world can own its own fade objects without a prefab.
        public sealed class RiftBurstFade : MonoBehaviour
        {
            private float life = 1.6f;
            private LineRenderer line;
            private void Awake() { line = GetComponent<LineRenderer>(); }
            private void Update()
            {
                life -= Time.deltaTime;
                if (line != null)
                {
                    line.widthMultiplier = Mathf.Max(0f, life * .55f + .08f);
                    transform.Rotate(0f, 220f * Time.deltaTime, 0f, Space.Self);
                    float s = Mathf.Lerp(1.25f, 1f, life / 1.6f);
                    transform.localScale = new Vector3(s, 1f, s);
                }
                if (life <= 0f) Destroy(gameObject);
            }
        }

        private IEnumerable<int> Neighbors(int id)
        {
            int x = id % Grid, z = id / Grid;
            if (x > 0) yield return id - 1;
            if (x < Grid - 1) yield return id + 1;
            if (z > 0) yield return id - Grid;
            if (z < Grid - 1) yield return id + Grid;
        }
        private bool Connected(int a, int b)
        {
            if (!nodes.ContainsKey(b) || Mathf.Abs(nodes[a].y - nodes[b].y) > .95f) return false;
            Vector3 delta = nodes[b] - nodes[a];
            return !Physics.SphereCast(nodes[a] + Vector3.up * .8f, .32f, delta.normalized, out _, delta.magnitude, 1 << TerrainLayer);
        }
        public float StartingYaw => Mathf.Atan2(ArenaCenter.x-spawn.x, ArenaCenter.z-spawn.z) * Mathf.Rad2Deg;
        private float SpawnScore(Vector3 point)
        {
            float score = -Mathf.Abs(new Vector2(point.x, point.z).magnitude - 18f);
            Vector3 focus = point + Vector3.up * 1.2f;
            float yaw = Mathf.Atan2(-point.x, -point.z) * Mathf.Rad2Deg;
            Vector3 behind = Quaternion.Euler(32, yaw, 0) * Vector3.back;
            score += Physics.SphereCast(focus, .3f, behind, out RaycastHit hit, 8, 1 << TerrainLayer) ? hit.distance * 4 : 32;
            return score;
        }
        private static bool IsArenaFloor(string name)
        {
            string n=name.ToLowerInvariant();
            return n.Contains("ground") || n.Contains("sand_island") || n.Contains("floor") || n.Contains("dirt_patch") || n.Contains("pathstone") || n.Contains("hill") || n.Contains("rockcluster") || n.Contains("fillrock");
        }
        private bool SpawnClear(Vector3 p) => !Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.1f,.4f,1<<TerrainLayer);
        private void BuildNavigation(float top)
        {
            nodes.Clear(); reachable.Clear();
            for (int z = 0; z < Grid; z++) for (int x = 0; x < Grid; x++)
            {
                Vector3 from = new Vector3((x - 40) * 1.05f, top, (z - 40) * 1.05f);
                if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, top + 10, 1 << TerrainLayer) && IsArenaFloor(hit.collider.name) && InsideSpawnArea(hit.point) && hit.normal.y > .82f && !Physics.CheckCapsule(hit.point + Vector3.up * .55f, hit.point + Vector3.up * (PlayerHeight - .32f), .32f, 1 << TerrainLayer))
                    nodes[x + z * Grid] = hit.point;
            }
            var visited = new HashSet<int>();
            foreach (int id in nodes.Keys)
            {
                if (!visited.Add(id)) continue;
                var region = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(id);
                while (queue.Count > 0)
                {
                    int here = queue.Dequeue(); region.Add(here);
                    foreach (int neighbor in Neighbors(here))
                        if (!visited.Contains(neighbor) && Connected(here, neighbor)) { visited.Add(neighbor); queue.Enqueue(neighbor); }
                }
                int RegionScore(IEnumerable<int> ids) => ids.Sum(n => Mathf.Abs(nodes[n].x) < 30 && Mathf.Abs(nodes[n].z) < 30 ? 4 : 1);
                if (region.Count >= 20 && RegionScore(region) > RegionScore(reachable)) { reachable.Clear(); reachable.UnionWith(region); }
            }
        }

        public Queue<Vector3> RouteTo(WildCreature creature)
        {
            int start = reachable.OrderBy(n => (nodes[n] - tamer.transform.position).sqrMagnitude).First();
            var queue = new Queue<int>(); var previous = new Dictionary<int, int> { [start] = start }; queue.Enqueue(start);
            while (queue.Count > 0 && !previous.ContainsKey(creature.node))
            {
                int here = queue.Dequeue();
                foreach (int next in Neighbors(here))
                    if (reachable.Contains(next) && !previous.ContainsKey(next) && Connected(here, next)) { previous[next] = here; queue.Enqueue(next); }
            }
            var route = new List<Vector3>();
            if (!previous.ContainsKey(creature.node)) return new Queue<Vector3>();
            int current = creature.node;
            while (current != start) { route.Add(nodes[current]); current = previous[current]; }
            route.Reverse(); return new Queue<Vector3>(route);
        }

        public void ShowBattle(OwnedCreature friendly, OwnedCreature enemy, int stage = 0)
        {
            if (terrainRoot != null) terrainRoot.SetActive(false);
            if (presentationRoot != null) { presentationRoot.SetActive(false); Object.Destroy(presentationRoot); }
            presentationRoot = new GameObject("Turn based battle stage");

            Vector3 playerSpawn = new Vector3(-2.5f, 0, 0);
            Vector3 enemySpawn  = new Vector3( 2.5f, 0, 2);
            bool mapSpawned = false;

            GameObject[] source = (catalog != null) ? catalog.battleMaps : null;
            if (source != null && source.Length > 0)
            {
                int stageIndex = Mathf.Clamp(stage, 0, source.Length - 1);
                GameObject mapPrefab = source[stageIndex];
                if (mapPrefab != null)
                {
                    GameObject map = Object.Instantiate(mapPrefab, presentationRoot.transform);
                    map.name = "BattleArena_" + (stageIndex + 1);
                    DisableImportedSceneComponents(map);
                    foreach (Collider col in map.GetComponentsInChildren<Collider>()) col.enabled = false;
                    Renderer[] renderers = map.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds bounds = BoundsOf(renderers);
                        float widest = Mathf.Max(bounds.size.x, bounds.size.z, 1f);
                        float fit = 22f / widest;
                        map.transform.localScale *= fit;
                        bounds = BoundsOf(map.GetComponentsInChildren<Renderer>());
                        map.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                        FixMaterials(map.GetComponentsInChildren<Renderer>());

                        Transform playerPlatform   = FindDeepChild(map.transform, "PlayerPlatform_Top");
                        Transform opponentPlatform = FindDeepChild(map.transform, "OpponentPlatform_Top");
                        if (playerPlatform != null)   playerSpawn = playerPlatform.position + Vector3.up * 0.05f;
                        if (opponentPlatform != null) enemySpawn  = opponentPlatform.position + Vector3.up * 0.05f;
                        mapSpawned = true;
                    }
                }
            }

            if (!mapSpawned)
                Platform("Battle floor", presentationRoot.transform, new Vector3(0, -.25f, 1), new Vector3(22, .5f, 18), new Color(.07f, .085f, .14f));

            float modelSize = mapSpawned ? 1.1f : 2.6f;
            battleTamer = Model(friendly.species.model, presentationRoot.transform, playerSpawn, modelSize).transform;
            battleEnemy = Model(enemy.species.model, presentationRoot.transform, enemySpawn, modelSize).transform;
            battleTamerHome = playerSpawn;
            battleEnemyHome = enemySpawn;
            battleTamer.LookAt(new Vector3(enemySpawn.x, playerSpawn.y, enemySpawn.z));
            battleEnemy.LookAt(new Vector3(playerSpawn.x, enemySpawn.y, playerSpawn.z));
            Ring(battleTamer, new Color(.2f, .9f, .7f)); Ring(battleEnemy, new Color(.8f, .2f, 1f));

            Vector3 forward  = (enemySpawn - playerSpawn); forward.y = 0;
            Vector3 fwdDir   = forward.normalized;
            Vector3 side     = Vector3.Cross(Vector3.up, fwdDir);
            if (mapSpawned)
            {
                // Classic Valmon over-the-shoulder: behind + slightly left of the player,
                // low to the ground, looking across at the opponent.
                camera.transform.position = playerSpawn
                                            - fwdDir * 2.6f           // behind the player
                                            + side  * 0.9f            // offset left/right of center
                                            + Vector3.up * 1.6f;      // low height
                camera.transform.LookAt(enemySpawn + Vector3.up * 0.4f);
            }
            else
            {
                Vector3 midpoint = (playerSpawn + enemySpawn) * 0.5f;
                camera.transform.position = midpoint + side * 7f + Vector3.up * 4.8f - fwdDir * 2f;
                camera.transform.LookAt(midpoint + Vector3.up * 1.2f);
            }
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
        public void ResumeMap()
        {
            if (presentationRoot != null) { presentationRoot.SetActive(false); Object.Destroy(presentationRoot); presentationRoot = null; }
            if (terrainRoot != null) terrainRoot.SetActive(true);
        }
        public void ResetPlayer()
        {
            controller.enabled = false; tamer.transform.position = spawn; controller.enabled = true;
        }
        public void Clear()
        {
            if (terrainRoot != null) { terrainRoot.SetActive(false); Object.Destroy(terrainRoot); }
            if (presentationRoot != null) { presentationRoot.SetActive(false); Object.Destroy(presentationRoot); }
            terrainRoot = presentationRoot = null;
            foreach (Material material in materials) Object.Destroy(material);
            materials.Clear(); wild.Clear(); nodes.Clear(); reachable.Clear();
            skyDrifters.Clear(); towerBeacon = null; ambientClock = 0f;
            waterRenderers.Clear(); lavaRenderers.Clear(); grassRenderers.Clear();
        }
        public void Dispose() => Clear();
    }
}

