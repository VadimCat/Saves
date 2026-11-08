#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Services.Saves.Editor
{
    public static class SaveTools
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "saveData.json");

        [MenuItem("Game/Save/Reset Save... %#r")]
        private static void ResetSave()
        {
            if (EditorApplication.isPlaying)
            {
                if (!EditorUtility.DisplayDialog(
                        "Play Mode is active",
                        "Stop Play Mode and remove save data?",
                        "Yes",
                        "Cancel"))
                {
                    return;
                }

                EditorApplication.isPlaying = false;
            }

            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();

                EditorUtility.DisplayDialog("Done", "Save removed.", "OK");
                Debug.Log($"[SaveTools] Save removed: {SavePath}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
                Debug.LogError($"[SaveTools] Error: {e}");
            }
        }

        [MenuItem("Game/Save/Open Save Folder")]
        private static void OpenSaveFolder()
        {
            var dir = Application.persistentDataPath;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            EditorUtility.RevealInFinder(dir);
        }

        [MenuItem("Game/Save/Print Save Path")]
        private static void PrintPath()
        {
            Debug.Log($"persistentDataPath: {Application.persistentDataPath}\nfile: {SavePath}");
        }
    }
}
#endif
