#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Services.Saves.Editor
{
    [InitializeOnLoad]
    public static class SaveTools
    {
        private static readonly HashSet<string> FileNames = new() { "saveData.json" };
        private const string PendingResetKey="Ji2.Saves.PendingEditorReset";
        private static string SavePath => Path.Combine(Application.persistentDataPath, "saveData.json");

        static SaveTools()
        {
            EditorApplication.playModeStateChanged+=AfterPlayMode;
            if(SessionState.GetBool(PendingResetKey,false) && !EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.delayCall+=CompletePendingReset;
        }

        public static void RegisterSaveFile(string fileName)
        {
            if(string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName)!=fileName)
                throw new ArgumentException("A save file name must not contain a directory.",nameof(fileName));
            FileNames.Add(fileName);
        }

        [MenuItem("Game/Save/Reset Save %#r")]
        private static void ResetSave()
        {
            if(SessionState.GetBool(PendingResetKey,false))return;
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(PendingResetKey,true);
                EditorApplication.isPlaying=false;
                return;
            }
            DeleteCurrentSave();
        }

        private static void AfterPlayMode(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=CompletePendingReset;
        }

        private static void CompletePendingReset()
        {
            if(!SessionState.GetBool(PendingResetKey,false) || EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.EraseBool(PendingResetKey);
            DeleteCurrentSave();
        }

        private static void DeleteCurrentSave()
        {
            try
            {
                DeleteSaveFiles(Application.persistentDataPath);
                PlayerPrefs.DeleteAll();PlayerPrefs.Save();
                Debug.Log($"[SaveTools] Save removed: {SavePath}");
            }
            catch(Exception e){Debug.LogError($"[SaveTools] Error: {e}");}
        }

        public static void DeleteSaveFiles(string directory)
        {
            foreach(var fileName in FileNames)
            foreach(var suffix in new[]{"",".bak",".tmp"})
            {
                var path=Path.Combine(directory,fileName+suffix);
                if(File.Exists(path))File.Delete(path);
            }
        }

        [MenuItem("Game/Save/Open Save Folder")]
        private static void OpenSaveFolder()
        {
            var dir=Application.persistentDataPath;
            if(!Directory.Exists(dir))Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        [MenuItem("Game/Save/Print Save Path")]
        private static void PrintPath()=>Debug.Log($"persistentDataPath: {Application.persistentDataPath}\nfile: {SavePath}");
    }
}
#endif
