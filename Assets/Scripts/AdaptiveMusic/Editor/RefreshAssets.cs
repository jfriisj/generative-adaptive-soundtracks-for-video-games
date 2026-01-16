using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace AdaptiveMusic.Editor
{
    public static class RefreshAssets
    {
        [MenuItem("Adaptive Music/Refresh Assets")]
        public static void RefreshDatabase()
        {
            Debug.Log("[RefreshAssets] Refreshing Unity Asset Database...");
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset("Assets/Scripts/AdaptiveMusic", ImportAssetOptions.ImportRecursive);
            Debug.Log("[RefreshAssets] Asset refresh complete");
        }

        [MenuItem("Adaptive Music/Reimport Scripts")]
        public static void ReimportScripts()
        {
            Debug.Log("[RefreshAssets] Reimporting AdaptiveMusic scripts...");
            AssetDatabase.ImportAsset("Assets/Scripts/AdaptiveMusic", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
            Debug.Log("[RefreshAssets] Script reimport complete");
        }
    }
}