using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Editor
{
    public static class SetupURP
    {
        [MenuItem("Tools/Setup URP Pipeline")]
        public static void CreateAndAssignURP()
        {
            string settingsDir = "Assets/_Game/Settings";
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
            }

            string rendererPath = $"{settingsDir}/URP_Renderer.asset";
            string urpAssetPath = $"{settingsDir}/URP_Asset.asset";

            // 1. Create or load Renderer Data
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            // 2. Create or load URP Pipeline Asset
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            }

            // 3. Assign as default render pipeline in Graphics and Quality settings
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SetupURP] Successfully configured and assigned URP Asset at {urpAssetPath}!");
        }
    }
}
