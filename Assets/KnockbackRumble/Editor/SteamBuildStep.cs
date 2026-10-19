using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KnockbackRumble.EditorTools
{
    /// <summary>
    /// After a Windows/Mac/Linux build, drops steam_appid.txt (480 = Valve's test app)
    /// next to the game so Steam features work when you or a friend run the build
    /// without launching it from Steam. Remove this once the game has its own Steam app.
    /// </summary>
    public class SteamBuildStep : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            var target = report.summary.platform;
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneWindows &&
                target != BuildTarget.StandaloneOSX && target != BuildTarget.StandaloneLinux64) return;

            string output = report.summary.outputPath;
            string folder = Directory.Exists(output) ? output : Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                File.WriteAllText(Path.Combine(folder, "steam_appid.txt"), "480");
                Debug.Log("[Knockback Rumble] Wrote steam_appid.txt next to the build.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Knockback Rumble] Couldn't write steam_appid.txt: " + e.Message);
            }
        }
    }
}
