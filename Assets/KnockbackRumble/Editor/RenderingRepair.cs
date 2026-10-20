using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace KnockbackRumble.EditorTools
{
    /// <summary>
    /// Fixes "everything is pink". Pink means Unity can't draw a material, which happens when:
    ///  1. the project lost its render pipeline (URP) setting, or
    ///  2. a material's shader went missing or broken.
    /// Menu: Knockback Rumble > Fix Pink Materials. It also runs a quick check (no changes)
    /// every time the editor loads and warns in the Console if the pipeline is missing.
    /// </summary>
    [InitializeOnLoad]
    public static class RenderingRepair
    {
        const string PcPipeline = "Assets/Settings/PC_RPAsset.asset";
        const string MaterialsFolder = "Assets/KnockbackRumble/Materials";
        const string LitShader = "Universal Render Pipeline/Lit";

        static RenderingRepair()
        {
            EditorApplication.delayCall += () =>
            {
                if (GraphicsSettings.currentRenderPipeline == null)
                    Debug.LogWarning("[Knockback Rumble] No render pipeline is set, so the course will look pink. " +
                                     "Run Knockback Rumble > Fix Pink Materials.");
            };
        }

        [MenuItem("Knockback Rumble/Fix Pink Materials", false, 60)]
        public static void Fix()
        {
            var report = new StringBuilder();

            // 1. Render pipeline (Graphics + every Quality level).
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcPipeline);
            if (pipeline == null)
            {
                report.AppendLine("Couldn't find " + PcPipeline + ". Is the Assets/Settings folder still there?");
            }
            else
            {
                if (GraphicsSettings.defaultRenderPipeline == null)
                {
                    GraphicsSettings.defaultRenderPipeline = pipeline;
                    report.AppendLine("Graphics render pipeline was EMPTY. Set it to PC_RPAsset.");
                }
                else report.AppendLine("Graphics render pipeline OK (" + GraphicsSettings.defaultRenderPipeline.name + ").");

                int current = QualitySettings.GetQualityLevel();
                string[] levels = QualitySettings.names;
                for (int i = 0; i < levels.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    if (QualitySettings.renderPipeline == null)
                    {
                        QualitySettings.renderPipeline = pipeline;
                        report.AppendLine("Quality level '" + levels[i] + "' had no pipeline. Set it to PC_RPAsset.");
                    }
                }
                QualitySettings.SetQualityLevel(current, false);
                AssetDatabase.SaveAssets();
            }

            // 2. Course materials: make sure each one uses the URP Lit shader.
            var lit = Shader.Find(LitShader);
            if (lit == null)
            {
                report.AppendLine("The URP Lit shader is missing. Open Window > Package Manager and make sure " +
                                  "'Universal RP' is installed (reinstall it if it has errors).");
            }
            else if (AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                int fixedCount = 0, total = 0;
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder }))
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (mat == null) continue;
                    total++;
                    if (mat.shader == lit && mat.shader.isSupported) continue;
                    Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                                : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.gray;
                    mat.shader = lit;
                    Mats.SetColor(mat, color);
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
                AssetDatabase.SaveAssets();
                report.AppendLine(fixedCount == 0
                    ? "All " + total + " course materials already use URP Lit."
                    : "Fixed " + fixedCount + " of " + total + " course materials (gave them the URP Lit shader).");
            }

            string text = report.ToString().Trim();
            Debug.Log("[Knockback Rumble] Fix Pink Materials:\n" + text);
            EditorUtility.DisplayDialog("Fix Pink Materials", text +
                "\n\nIf the course is still pink, run Knockback Rumble > Build Scrap Sprint Scene, " +
                "and send Claude this message.", "OK");
        }
    }
}
