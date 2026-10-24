using UnityEditor;
using UnityEngine;
using Valmon.Campaign;

namespace Valmon.CampaignEditor
{
    // Auto-populates CampaignCatalog.battleMaps with the three Blender battle scenes
    // whenever the Editor loads or scripts recompile. The native fileID that Unity
    // assigns to a .blend's root GameObject differs from the .fbx root, so hand-written
    // GUID+fileID references in the .asset YAML can't be guessed - this script lets
    // AssetDatabase resolve the correct references and writes them back to the catalog.
    [InitializeOnLoad]
    public static class BattleMapsAutoWire
    {
        private const string CatalogPath = "Assets/Campaign/CampaignCatalog.asset";
        private static readonly string[] BattleMapPaths =
        {
            "Assets/Scenes/BlenderMaps/PokemonBattleScene.blend",
            "Assets/Scenes/BlenderMaps/CaveBattleScene.blend",
            "Assets/Scenes/BlenderMaps/SandyBeachScene.blend",
        };

        static BattleMapsAutoWire()
        {
            EditorApplication.delayCall += Apply;
        }

        [MenuItem("Tools/VALMON/Wire Battle Maps")]
        public static void Apply()
        {
            CampaignCatalog catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning("[BattleMapsAutoWire] CampaignCatalog not found at " + CatalogPath);
                return;
            }

            GameObject[] desired = new GameObject[BattleMapPaths.Length];
            for (int i = 0; i < BattleMapPaths.Length; i++)
            {
                desired[i] = AssetDatabase.LoadAssetAtPath<GameObject>(BattleMapPaths[i]);
                if (desired[i] == null)
                    Debug.LogWarning("[BattleMapsAutoWire] Missing map asset: " + BattleMapPaths[i]);
            }

            bool matches = catalog.battleMaps != null && catalog.battleMaps.Length == desired.Length;
            if (matches)
                for (int i = 0; i < desired.Length; i++)
                    if (catalog.battleMaps[i] != desired[i]) { matches = false; break; }

            AudioClip introClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pokemon game intro.mp3");
            AudioClip soundtrackClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Full game sound trak.mp3");
            AudioClip battleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pokemon battling.mp3");
            AudioClip victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Pokemon Victory Theme.mp3");
            AudioClip defeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/battle lost.mp3");
            AudioClip catchClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pokeom caught.mp3");
            AudioClip corruptClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/corruption pokemon.mp3");

            bool audioChanged = false;
            if (catalog.menuMusic != introClip && introClip != null) { catalog.menuMusic = introClip; audioChanged = true; }
            if (catalog.introMusic != introClip && introClip != null) { catalog.introMusic = introClip; audioChanged = true; }
            if (catalog.gameSoundtrack != soundtrackClip && soundtrackClip != null) { catalog.gameSoundtrack = soundtrackClip; audioChanged = true; }
            if (catalog.battleMusic != battleClip && battleClip != null) { catalog.battleMusic = battleClip; audioChanged = true; }
            if (catalog.victoryMusic != victoryClip && victoryClip != null) { catalog.victoryMusic = victoryClip; audioChanged = true; }
            if (catalog.battleLostAudio != defeatClip && defeatClip != null) { catalog.battleLostAudio = defeatClip; audioChanged = true; }
            if (catalog.captureAudio != catchClip && catchClip != null) { catalog.captureAudio = catchClip; audioChanged = true; }
            if (catalog.corruptionAudio != corruptClip && corruptClip != null) { catalog.corruptionAudio = corruptClip; audioChanged = true; }

            if (matches && !audioChanged) return;

            Undo.RecordObject(catalog, "Wire Battle Maps & Audio");
            if (!matches) catalog.battleMaps = desired;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("[BattleMapsAutoWire] CampaignCatalog wired with battle maps and audio tracks.");
        }
    }
}
