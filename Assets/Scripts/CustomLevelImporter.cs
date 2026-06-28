using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace KillerMineDoku.UI
{
    public static class CustomLevelImporter
    {
        public static bool TryImportJson(out string json, out string message)
        {
            json = string.Empty;
            message = string.Empty;

#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("\u5bfc\u5165\u81ea\u5236\u5173\u5361", string.Empty, "json");
            if (string.IsNullOrWhiteSpace(path))
            {
                message = string.Empty;
                return false;
            }

            try
            {
                json = File.ReadAllText(path);
                return true;
            }
            catch (IOException ex)
            {
                message = $"\u8bfb\u53d6\u6587\u4ef6\u5931\u8d25\n{ex.Message}";
                return false;
            }
#elif UNITY_STANDALONE_WIN
            return TryImportJsonFromFolder(out json, out message);
#else
            message = "\u5f53\u524d\u5e73\u53f0\u6682\u4e0d\u652f\u6301\u5bfc\u5165\u81ea\u5236\u5173\u5361";
            return false;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        public static void RequestWebJsonImport(string targetObjectName, string successMethod, string failureMethod)
        {
            KillerMineDoku_RequestJsonImport(targetObjectName, successMethod, failureMethod);
        }

        [DllImport("__Internal")]
        private static extern void KillerMineDoku_RequestJsonImport(string targetObjectName, string successMethod, string failureMethod);
#endif

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static bool TryImportJsonFromFolder(out string json, out string message)
        {
            json = string.Empty;
            message = string.Empty;

            try
            {
                var importFolder = GetImportFolder();
                Directory.CreateDirectory(importFolder);
                OpenImportFolder(importFolder);

                var files = Directory.GetFiles(importFolder, "*.json", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    message = $"\u5df2\u6253\u5f00\u5bfc\u5165\u6587\u4ef6\u5939\n\u8bf7\u5c06 .json \u5173\u5361\u6587\u4ef6\u653e\u5165\u8be5\u6587\u4ef6\u5939\uff0c\u7136\u540e\u56de\u5230\u6e38\u620f\u518d\u70b9\u4e00\u6b21\u5bfc\u5165\u3002\n{importFolder}";
                    return false;
                }

                System.Array.Sort(files, (left, right) =>
                    File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left)));

                var path = files[0];
                json = File.ReadAllText(path);
                return true;
            }
            catch (IOException ex)
            {
                message = $"\u8bfb\u53d6\u6587\u4ef6\u5931\u8d25\n{ex.Message}";
                return false;
            }
            catch (System.UnauthorizedAccessException ex)
            {
                message = $"\u8bfb\u53d6\u6587\u4ef6\u5931\u8d25\n{ex.Message}";
                return false;
            }
        }

        private static string GetImportFolder()
        {
            return Path.Combine(Application.persistentDataPath, "ImportLevels");
        }

        private static void OpenImportFolder(string importFolder)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = importFolder,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"\u6253\u5f00\u5bfc\u5165\u6587\u4ef6\u5939\u5931\u8d25\n{ex.Message}");
            }
        }
#endif
    }
}
