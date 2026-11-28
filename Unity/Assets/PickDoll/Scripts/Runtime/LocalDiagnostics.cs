using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PickDoll
{
    // Two 512 KiB files, retained across sessions; no per-frame logging or network upload.
    public static class LocalDiagnostics
    {
        public const int FileLimit = 512 * 1024;
        static readonly string Session = Guid.NewGuid().ToString("N").Substring(0, 8);
        static readonly object Gate = new object();
        static bool disabled;
        public static string DirectoryPath => Path.Combine(Application.persistentDataPath, "diagnostics");
        public static void Write(string kind, string details)
        {
            if (disabled) return;
            try
            {
                string line = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz") +
                    " session=" + Session + " elapsed=" + Time.realtimeSinceStartupAsDouble.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                    " event=" + kind + " " + details.Replace('\n', ' ').Replace('\r', ' ') + "\n";
                lock (Gate)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    string path = Path.Combine(DirectoryPath, "current.log");
                    if (File.Exists(path) && new FileInfo(path).Length + Encoding.UTF8.GetByteCount(line) > FileLimit)
                    {
                        string previous = Path.Combine(DirectoryPath, "previous.log");
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(path, previous);
                    }
                    File.AppendAllText(path, line, new UTF8Encoding(false));
                }
            }
            catch (Exception e)
            {
                disabled = true;
                Debug.LogWarning("PickDoll diagnostics disabled: " + e.GetType().Name);
            }
        }
    }
}
