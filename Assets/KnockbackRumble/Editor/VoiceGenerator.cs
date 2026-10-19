using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace KnockbackRumble.EditorTools
{
    /// <summary>
    /// Menu: Knockback Rumble > Voices
    ///  - Generate Placeholder Voices: speaks every line with your computer's built-in
    ///    text-to-speech (Windows or Mac) and saves it to Assets/KnockbackRumble/Resources/VO.
    ///    Only fills in missing files, so real recordings you drop in are never overwritten.
    ///  - Export Voice Script: a CSV of every line with its file name, for recording real voices.
    /// To replace a placeholder, record a .wav with the same file name and drop it in the VO folder.
    /// </summary>
    public static class VoiceGenerator
    {
        const string VoFolder = "Assets/KnockbackRumble/Resources/VO";

        [MenuItem("Knockback Rumble/Voices/Generate Placeholder Voices", false, 20)]
        public static void GenerateMissing() => Generate(false);

        [MenuItem("Knockback Rumble/Voices/Regenerate ALL Placeholder Voices", false, 21)]
        public static void RegenerateAll()
        {
            if (EditorUtility.DisplayDialog("Regenerate all voices?",
                    "This overwrites every file in " + VoFolder + ", including any real recordings you added.",
                    "Overwrite everything", "Cancel"))
                Generate(true);
        }

        [MenuItem("Knockback Rumble/Voices/Export Voice Script (CSV)", false, 40)]
        public static void ExportScript()
        {
            string path = EditorUtility.SaveFilePanel("Save voice script", ProjectRoot, "KnockbackRumble_VoiceScript", "csv");
            if (string.IsNullOrEmpty(path)) return;
            var sb = new StringBuilder("file,speaker,line\n");
            foreach (var line in AllLines())
                sb.Append(line.id).Append(".wav,").Append(Csv(line.speaker)).Append(',').Append(Csv(line.text)).Append('\n');
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            EditorUtility.RevealInFinder(path);
        }

        public static List<VoiceLine> AllLines()
        {
            var lines = new List<VoiceLine>(Announcer.AllLines());
            foreach (var p in Personas.Bots)
            {
                lines.AddRange(p.Launched);
                lines.AddRange(p.Fell);
                lines.AddRange(p.Finished);
            }
            return lines;
        }

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

        static void Generate(bool overwrite)
        {
            string folder = Path.Combine(ProjectRoot, VoFolder);
            Directory.CreateDirectory(folder);

            var todo = new List<VoiceLine>();
            foreach (var line in AllLines())
            {
                string wav = Path.Combine(folder, line.id + ".wav");
                if (overwrite || !File.Exists(wav)) todo.Add(line);
            }
            if (todo.Count == 0)
            {
                EditorUtility.DisplayDialog("Voices", "All voice lines already exist. Nothing to do.", "OK");
                return;
            }

            bool windows = Application.platform == RuntimePlatform.WindowsEditor;
            bool mac = Application.platform == RuntimePlatform.OSXEditor;
            if (!windows && !mac)
            {
                EditorUtility.DisplayDialog("Voices", "Placeholder voices need Windows or Mac text-to-speech.", "OK");
                return;
            }

            string tempDir = Path.Combine(ProjectRoot, "Temp");
            Directory.CreateDirectory(tempDir);
            string script = Path.Combine(tempDir, windows ? "kr_voices.ps1" : "kr_voices.sh");
            File.WriteAllText(script, windows ? WindowsScript(todo, folder) : MacScript(todo, folder), new UTF8Encoding(windows));

            try
            {
                EditorUtility.DisplayProgressBar("Knockback Rumble", "Speaking " + todo.Count + " voice lines... (this takes a minute)", 0.5f);
                var psi = new ProcessStartInfo
                {
                    FileName = windows ? "powershell.exe" : "/bin/bash",
                    Arguments = windows ? "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"" : "\"" + script + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                };
                using (var proc = Process.Start(psi))
                {
                    string err = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(10 * 60 * 1000);
                    if (!string.IsNullOrWhiteSpace(err)) Debug.LogWarning("[Knockback Rumble] Text-to-speech said:\n" + err);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Knockback Rumble] Could not run text-to-speech: " + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            int made = 0;
            foreach (var line in todo)
                if (File.Exists(Path.Combine(folder, line.id + ".wav"))) made++;
            Debug.Log("[Knockback Rumble] Generated " + made + " of " + todo.Count + " voice lines in " + VoFolder);
            EditorUtility.DisplayDialog("Voices", "Generated " + made + " of " + todo.Count + " voice lines.\nPress Play to hear them.", "OK");
        }

        // ---------- Windows: System.Speech through PowerShell ----------

        static string WindowsScript(List<VoiceLine> lines, string folder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Add-Type -AssemblyName System.Speech");
            sb.AppendLine("$s = New-Object System.Speech.Synthesis.SpeechSynthesizer");
            foreach (var line in lines)
            {
                string gender = line.style.female ? "Female" : "Male";
                string wav = Path.Combine(folder, line.id + ".wav");
                string ssml = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" +
                              "<prosody pitch=\"" + line.style.pitch + "\">" + Xml(line.text) + "</prosody></speak>";
                sb.AppendLine("try {");
                sb.AppendLine("  $s.SelectVoiceByHints([System.Speech.Synthesis.VoiceGender]::" + gender + ")");
                sb.AppendLine("  $s.Rate = " + Mathf.Clamp(line.style.rate, -10, 10));
                sb.AppendLine("  $s.SetOutputToWaveFile(" + Ps(wav) + ")");
                sb.AppendLine("  $s.SpeakSsml(" + Ps(ssml) + ")");
                sb.AppendLine("} catch { Write-Error (\"" + line.id + ": \" + $_) }");
                sb.AppendLine("$s.SetOutputToNull()");
            }
            sb.AppendLine("$s.Dispose()");
            return sb.ToString();
        }

        // ---------- Mac: the built-in "say" command ----------

        static string MacScript(List<VoiceLine> lines, string folder)
        {
            var sb = new StringBuilder("#!/bin/bash\n");
            foreach (var line in lines)
            {
                string voice = line.style.female ? "Samantha" : "Fred";
                int wpm = 175 + line.style.rate * 15;
                string wav = Path.Combine(folder, line.id + ".wav");
                sb.Append("say -v ").Append(voice).Append(" -r ").Append(wpm)
                  .Append(" --file-format=WAVE --data-format=LEI16@22050 -o ").Append(Sh(wav))
                  .Append(' ').Append(Sh(line.text)).Append(" || say -r ").Append(wpm)
                  .Append(" --file-format=WAVE --data-format=LEI16@22050 -o ").Append(Sh(wav))
                  .Append(' ').Append(Sh(line.text)).Append('\n');
            }
            return sb.ToString();
        }

        // ---------- Escaping ----------

        static string Xml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                                         .Replace("\"", "&quot;").Replace("'", "&apos;");
        static string Ps(string s) => "'" + s.Replace("'", "''") + "'";
        static string Sh(string s) => "'" + s.Replace("'", "'\\''") + "'";
        static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
