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

            if (matches) return;

            Undo.RecordObject(catalog, "Wire Battle Maps");
            catalog.battleMaps = desired;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("[BattleMapsAutoWire] CampaignCatalog.battleMaps -> [grass, cave, beach]");
        }
    }
}
