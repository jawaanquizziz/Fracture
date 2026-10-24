using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Valmon.Campaign;

namespace Valmon.Editor
{
    public static class CampaignBuilder
    {
        public const string ScenePath = "Assets/Scenes/VALMON_Campaign.unity";
        public const string CatalogPath = "Assets/Campaign/CampaignCatalog.asset";
        private static MoveDefinition M(string name, string type, int power, bool special = false, int pp = 20, string effect = "", int accuracy = 100)
            => new MoveDefinition { name = name, type = type, power = power, special = special, pp = pp, effect = effect, accuracy = accuracy };
        private static SpeciesDefinition S(string id, string name, string types, int hp, int attack, int defense, int spAttack, int spDefense, int speed, params MoveDefinition[] moves)
            => new SpeciesDefinition { id = id, displayName = name, types = types.Split('/'), hp = hp, attack = attack, defense = defense, specialAttack = spAttack, specialDefense = spDefense, speed = speed, moves = moves };

        private static SpeciesDefinition[] Definitions() => new[]
        {
            S("bulbasaur", "Bulbasaur", "Grass/Poison", 45,49,49,65,65,45, M("Vine Whip","Grass",45), M("Tackle","Normal",40), M("Poison Powder","Poison",0,false,15,"poison",75), M("Growl","Normal",0,false,25,"weaken")),
            S("ivysaur", "Ivysaur", "Grass/Poison", 60,62,63,80,80,60, M("Razor Leaf","Grass",55,false,20,"critical",95), M("Acid","Poison",40,true), M("Mega Drain","Grass",40,true,15,"drain"), M("Synthesis","Grass",0,false,5,"heal")),
            S("venusaur", "Venusaur", "Grass/Poison", 80,82,83,100,100,80, M("Energy Ball","Grass",90,true,10), M("Sludge Bomb","Poison",90,true,10), M("Razor Leaf","Grass",55,false,20,"critical",95), M("Synthesis","Grass",0,false,5,"heal")),
            S("chikorita", "Chikorita", "Grass",45,49,65,49,65,45, M("Razor Leaf","Grass",55,false,20,"critical",95), M("Tackle","Normal",40), M("Poison Powder","Poison",0,false,15,"poison",75), M("Reflect","Psychic",0,false,10,"guard")),
            S("oddish", "Oddish", "Grass/Poison",45,50,55,75,65,30, M("Absorb","Grass",20,true,25,"drain"), M("Acid","Poison",40,true), M("Sleep Powder","Grass",0,false,10,"sleep",75), M("Moonlight","Fairy",0,false,5,"heal")),
            S("snivy", "Snivy", "Grass",45,45,55,45,55,63, M("Vine Whip","Grass",45), M("Leaf Blade","Grass",90,false,10,"critical"), M("Tackle","Normal",40), M("Coil","Poison",0,false,10,"guard")),
            S("charmander", "Charmander", "Fire",39,52,43,60,50,65, M("Scratch","Normal",40), M("Ember","Fire",40,true,25,"burn"), M("Dragon Breath","Dragon",60,true,15), M("Smokescreen","Normal",0,false,15,"blind")),
            S("charmeleon", "Charmeleon", "Fire",58,64,58,80,65,80, M("Fire Fang","Fire",65,false,15,"burn",95), M("Flamethrower","Fire",90,true,10,"burn"), M("Slash","Normal",70,false,15,"critical"), M("Growl","Normal",0,false,20,"weaken")),
            S("charizard", "Charizard", "Fire/Flying",78,84,78,109,85,100, M("Flamethrower","Fire",90,true,10,"burn"), M("Wing Attack","Flying",60), M("Dragon Claw","Dragon",80,false,15), M("Smokescreen","Normal",0,false,15,"blind")),
            S("cyndaquil", "Cyndaquil", "Fire",39,52,43,60,50,65, M("Ember","Fire",40,true,25,"burn"), M("Tackle","Normal",40), M("Flame Wheel","Fire",60,false,20,"burn"), M("Smokescreen","Normal",0,false,15,"blind")),
            S("growlithe", "Growlithe", "Fire",55,70,45,70,50,60, M("Bite","Dark",60), M("Flame Wheel","Fire",60,false,20,"burn"), M("Take Down","Normal",90,false,10,"recoil",85), M("Growl","Normal",0,false,20,"weaken")),
            S("vulpix", "Vulpix", "Fire",38,41,40,50,65,65, M("Ember","Fire",40,true,25,"burn"), M("Flamethrower","Fire",90,true,10,"burn"), M("Feint Attack","Dark",60), M("Tail Guard","Normal",0,false,15,"guard")),
            S("squirtle", "Squirtle", "Water",44,48,65,50,64,43, M("Water Gun","Water",40,true,25), M("Bite","Dark",60), M("Tackle","Normal",40), M("Withdraw","Water",0,false,20,"guard")),
            S("wartortle", "Wartortle", "Water",59,63,80,65,80,58, M("Water Pulse","Water",60,true), M("Bite","Dark",60), M("Ice Beam","Ice",90,true,10), M("Withdraw","Water",0,false,20,"guard")),
            S("blastoise", "Blastoise", "Water",79,83,100,85,105,78, M("Hydro Pump","Water",110,true,5,"",80), M("Water Pulse","Water",60,true), M("Ice Beam","Ice",90,true,10), M("Withdraw","Water",0,false,20,"guard")),
            S("megablastoise", "Mega Blastoise", "Water",79,103,120,135,115,78, M("Hydro Pump","Water",110,true,5,"",80), M("Dark Pulse","Dark",80,true,15), M("Aura Sphere","Fighting",80,true,15), M("Withdraw","Water",0,false,20,"guard")),
            S("lapras", "Lapras", "Water/Ice",130,85,80,85,95,60, M("Water Pulse","Water",60,true), M("Ice Beam","Ice",90,true,10), M("Body Slam","Normal",85,false,15), M("Sing","Normal",0,false,10,"sleep",55)),
            S("totodile", "Totodile", "Water",50,65,64,44,48,43, M("Water Gun","Water",40,true,25), M("Bite","Dark",60), M("Ice Fang","Ice",65,false,15,"",95), M("Leer","Normal",0,false,20,"weaken")),
            S("vaporeon", "Vaporeon", "Water",130,65,60,110,95,65, M("Water Pulse","Water",60,true), M("Aurora Beam","Ice",65,true), M("Bite","Dark",60), M("Aqua Ring","Water",0,false,5,"heal")),
            S("primalgroudon", "Primal Groudon", "Ground/Fire",100,150,140,120,90,90, M("Earthquake","Ground",100,false,10), M("Lava Plume","Fire",80,true,15,"burn"), M("Rock Slide","Rock",75,false,10,"",90), M("Bulk Up","Fighting",0,false,10,"guard")),
            S("lugia", "Lugia", "Psychic/Flying",106,90,130,90,154,110, M("Aeroblast","Flying",100,true,5,"critical",95), M("Psychic","Psychic",90,true,10), M("Ancient Power","Rock",60,true,10), M("Recover","Normal",0,false,5,"heal")),
            S("rayquaza", "Rayquaza", "Dragon/Flying",105,150,90,150,90,95, M("Dragon Pulse","Dragon",85,true,10), M("Air Slash","Flying",75,true,15,"",95), M("Crunch","Dark",80,false,15), M("Dragon Guard","Dragon",0,false,10,"guard"))
        };

        [MenuItem("Tools/VALMON/Build Campaign", false, 0)]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Campaign"); Directory.CreateDirectory("Assets/StreamingAssets");
            string video = File.Exists("Video/VALMON_Opening.mp4") ? "Video/VALMON_Opening.mp4" : "VALMON_Opening.mp4";
            if (!File.Exists(video)) throw new FileNotFoundException("Opening video missing", video);
            File.Copy(video, "Assets/StreamingAssets/VALMON_Opening.mp4", true);
            AssetDatabase.Refresh();
            var data = ScriptableObject.CreateInstance<CampaignCatalog>();
            if (File.Exists("Video/VALMON_Championship_Opening.mp4"))
            {
                File.Copy("Video/VALMON_Championship_Opening.mp4", "Assets/StreamingAssets/VALMON_Championship_Opening.mp4", true);
                data.introFile = "VALMON_Championship_Opening.mp4";
            }
            Directory.CreateDirectory("Assets/Audio");
            if (File.Exists("Video/valmon_soundtrack.wav")) File.Copy("Video/valmon_soundtrack.wav", "Assets/Audio/ValomonMenuTheme.wav", true);
            AssetDatabase.Refresh();
            data.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ValomonMenuTheme.wav");
            data.gameSoundtrack = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Full game sound trak.mpeg");
            data.battleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pokemon battling.mpeg");
            data.introMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pokemon game intro.mpeg");
            data.victoryMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Pokemon Victory Theme.mp3.mpeg");
            data.battleLostAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/battle lost.mpeg");
            data.captureAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pokeom caught.mpeg");
            data.corruptionAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/corruption pokemon.mpeg");
            data.species = Definitions(); data.characters = new CharacterDefinition[4]; data.maps = new GameObject[3];
            string[] assets = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/Characters/") && !p.Contains("/source/") && (p.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.Contains("/ImportedPokemon/") ? 0 : 1).ToArray();
            foreach (SpeciesDefinition species in data.species)
            {
                string path = assets.FirstOrDefault(p => Key(Path.GetFileNameWithoutExtension(p)) == species.id);
                if (path == null) throw new InvalidOperationException("Missing species model: " + species.displayName);
                species.model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (species.model == null) throw new InvalidOperationException("Model failed to import: " + path);
                if (species.id == "charmander") species.albedoOverride = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Characters/pokemon-charmander/textures/Charmander_texture.png");
                if (species.id == "lapras") species.albedoOverride = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Characters/pokemon-lapras/textures/lapras-texture.png");
                if (species.id == "rayquaza")
                {
                    GameObject direct = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/rayquaza-remastered/Rayquaza.gltf");
                    if (direct != null) species.model = direct;
                    species.albedoOverride = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Characters/rayquaza-remastered/textures/gltf_embedded_0.png");
                }
            }
            string[] characters = { "Clove", "Gekko", "Reyna", "Tejo" };
            for (int i = 0; i < characters.Length; i++) data.characters[i] = new CharacterDefinition { name = characters[i], yawOffset = i == 0 || i == 3 ? 180 : 0, model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/" + characters[i].ToLowerInvariant() + "__valorant.glb") };
            GameObject bakedReyna = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/ImportedCharacters/ReynaWalking.fbx");
            if (bakedReyna != null)
            {
                var reynaImporter = AssetImporter.GetAtPath("Assets/Characters/ImportedCharacters/ReynaWalking.fbx") as ModelImporter;
                string[] materialNames = { "TP_Core_Eye_MI_Reyna", "3P_Vampire_CS_FX_Inst", "3P_Vampire_CS_FX_Hair_Inst" };
                string[] images = { "Image_0.png", "Image_2.png", "Image_4.png" };
                Directory.CreateDirectory("Assets/Campaign/Materials");
                for (int i = 0; i < materialNames.Length; i++)
                {
                    string materialPath = "Assets/Campaign/Materials/" + materialNames[i] + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Characters/ImportedCharacters/ReynaTextures/" + images[i]));
                    material.SetFloat("_Smoothness", .3f); EditorUtility.SetDirty(material);
                    reynaImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialNames[i]), material);
                }
                reynaImporter.SaveAndReimport();
                bakedReyna = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/ImportedCharacters/ReynaWalking.fbx");
            }
            if (bakedReyna != null) data.characters[2].model = bakedReyna;
            for (int i = 0; i < 3; i++)
            {
                string mapPath = $"Assets/Maps/Map{i + 1}.fbx";
                // Runtime collision cooking needs CPU mesh data in a standalone player.
                // The Editor retains that data even when Read/Write is disabled.
                var importer = AssetImporter.GetAtPath(mapPath) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing map importer: " + mapPath);
                if (!importer.isReadable) { importer.isReadable = true; importer.SaveAndReimport(); }
                data.maps[i] = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
            }
            data.captureBall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/poke_ball.glb");
            data.surfaceShader = Shader.Find("Universal Render Pipeline/Lit");
            CampaignCatalog existing = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (existing == null) { AssetDatabase.CreateAsset(data, CatalogPath); existing = data; }
            else { EditorUtility.CopySerialized(data, existing); UnityEngine.Object.DestroyImmediate(data); }
            EditorUtility.SetDirty(existing); AssetDatabase.SaveAssets();
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("VALMON Campaign"); root.AddComponent<CampaignController>().catalog = existing;
            Light sun = new GameObject("Rift daylight").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.5f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            Light fill = new GameObject("Soft fill").AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .35f; fill.color = new Color(.55f,.7f,1);
            fill.transform.rotation = Quaternion.Euler(20, 140, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.48f,.53f,.65f); RenderSettings.ambientEquatorColor = new Color(.32f,.38f,.45f); RenderSettings.ambientGroundColor = new Color(.22f,.26f,.30f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            PlayerSettings.productName = "VALMON";
            PlayerSettings.runInBackground = true;
            Validate();
            Debug.Log("[VALMON] Campaign built. Intro, menus, 3 maps, 22 species, 4 characters.");
        }
        private static string Key(string name)
        {
            name = name.ToLowerInvariant().Replace(" ", "");
            if (name == "blastoiseMega".ToLowerInvariant()) return "megablastoise";
            return name;
        }

        [MenuItem("Tools/VALMON/Validate Campaign")]
        public static void Validate()
        {
            CampaignCatalog data = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var results = new List<string>();
            Action<bool, string> check = (condition, description) => { if (!condition) throw new Exception("Campaign validation failed: " + description); results.Add("PASS " + description); };
            check(data != null && data.species.Length == 22, "22 species registered");
            check(data.maps.Length == 3 && data.maps.All(m => m != null && m.GetComponentsInChildren<Renderer>(true).Length > 0), "3 original maps have renderable geometry");
            check(data.maps.All(m => m.GetComponentsInChildren<MeshFilter>(true).All(f => f.sharedMesh == null || f.sharedMesh.isReadable)), "Map meshes retain CPU data for standalone collision cooking");
            check(data.characters.Length == 4 && data.characters.All(c => c.model != null), "4 selectable characters");
            foreach (SpeciesDefinition species in data.species)
            {
                check(species.model.GetComponentsInChildren<Renderer>(true).Length > 0 && species.moves.Length == 4 && species.moves.All(m => m.pp > 0 && m.accuracy > 0 && m.accuracy <= 100), species.displayName + ": mesh and four valid moves");
            }
            var run = new CampaignRun(); run.party.Add(new OwnedCreature(data.Find("bulbasaur"), 10));
            int[] minimum = { 1, 16, 31 }, maximum = { 15, 30, 50 };
            for (int i = 0; i < 3; i++)
            {
                run.StartStage(i);
                check(run.MinLevel == minimum[i] && run.MaxLevel == maximum[i] && run.balls == CampaignRun.BallsPerStage && Mathf.Approximately(run.seconds, CampaignRun.SecondsPerStage), "Stage " + (i + 1) + " level band, timer, ball reset");
                check(data.species.Count(s => s.types.Contains(run.Habitat)) >= 6, "Stage " + (i + 1) + " typed spawn roster");
            }
            run.StartStage(0); run.catches = CampaignRun.RequiredCatches - 1; run.balls = 1;
            check(run.SpendBall() && run.Outcome(true) == StageOutcome.Playing, "Last ball may finish a pending encounter");
            run.Catch(new OwnedCreature(data.Find("oddish"), 5));
            check(run.Outcome(false) == StageOutcome.Cleared && !run.SpendBall(), "Final catch clears level and blocks extra capture");
            run.StartStage(0); run.catches=5;
            check(run.Outcome(false)==StageOutcome.Playing && run.SpendBall(), "Five catches allow continuing toward the six-catch bonus");
            run.catches=6; run.seconds=0;
            check(run.Outcome(false)==StageOutcome.Cleared, "Quota achieved wins when time expires");
            run.StartStage(0); run.balls = CampaignRun.RequiredCatches - 1;
            check(run.Outcome(false) == StageOutcome.NoBalls, "Impossible capture quota loses immediately");
            run.StartStage(0); run.seconds = 0;
            check(run.Outcome(true) == StageOutcome.TimeExpired, "Timer expires during encounters");
            run.StartStage(0); run.party.Clear();
            check(run.Outcome(false) == StageOutcome.PartyLost, "Empty team loses");
            check(BattleRules.Effectiveness("Water", data.Find("charmander")) == 2 && BattleRules.Effectiveness("Fire", data.Find("squirtle")) == .5f && BattleRules.Effectiveness("Ground", data.Find("rayquaza")) == 0, "Type strengths, resistance and immunity");
            var a = new OwnedCreature(data.Find("charmander"), 10); var b = new OwnedCreature(data.Find("bulbasaur"), 10); int hp = b.health;
            BattleRules.Attack(a, b, 1, new System.Random(5));
            check(b.health < hp && a.pp[1] == a.species.moves[1].pp - 1, "Attack deals damage and consumes PP");
            for (int seed = 0; seed < 120; seed++)
            {
                var random = new System.Random(seed);
                a = new OwnedCreature(data.species[random.Next(data.species.Length)], random.Next(1, 51));
                b = new OwnedCreature(data.species[random.Next(data.species.Length)], random.Next(1, 51));
                int turns = 0;
                while (a.health > 0 && b.health > 0 && turns < 500)
                {
                    if (turns % 2 == 0) BattleRules.Attack(a, b, BattleRules.BestMove(a, b, random), random);
                    else BattleRules.Attack(b, a, BattleRules.BestMove(b, a, random), random);
                    turns++;
                }
                if (turns == 500 || a.health < 0 || b.health < 0 || a.health > a.MaxHealth || b.health > b.MaxHealth) throw new Exception("Battle simulation failed at seed " + seed);
            }
            results.Add("PASS 120 seeded AI battles terminate with valid health");
            check(File.Exists("Assets/StreamingAssets/VALMON_Opening.mp4"), "Opening MP4 packaged");
            Directory.CreateDirectory("Tools/Reports"); File.WriteAllLines("Tools/Reports/campaign-validation.txt", results);
            Debug.Log("[VALMON] " + results.Count + " checks passed, including 120 battle simulations.");
            LanNetworkValidation.Run();
            CampaignController.ValidateControlPacketBudget();
        }

        [MenuItem("Tools/VALMON/Build Windows Player")]
        public static void BuildWindows()
        {
            Build();
            Directory.CreateDirectory("Builds/VALMON");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/VALMON/VALMON.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + report.summary.result);
            Debug.Log("[VALOMON] Windows player ready: Builds/VALMON/VALMON.exe");
        }

        [MenuItem("Tools/VALMON/Build Windows Release")]
        public static void BuildRelease()
        {
            Build(); Directory.CreateDirectory("Builds/VALMON");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/VALMON/VALMON.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Release build failed: " + report.summary.result);
            File.Copy("Docs/Multiplayer-Setup.md", "Builds/VALMON/Multiplayer-Setup.md", true);
            File.Copy("Tools/Allow-VALMON-LAN.ps1", "Builds/VALMON/Allow-VALMON-LAN.ps1", true);
            Debug.Log("[VALOMON] Windows release ready: Builds/VALMON/VALMON.exe");
        }

        public static void PlaySmoke()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}


