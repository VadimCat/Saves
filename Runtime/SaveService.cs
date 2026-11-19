using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Services.Saves
{
    public class SaveService<TSaveData> : ISaveService<TSaveData> where TSaveData : class, ISaveData, new()
    {
        // Shared so Newtonsoft keeps its per-type contract cache between saves.
        private static readonly JsonSerializerSettings SerializerSettings = CreateSerializerSettings();

        private readonly IReadOnlyList<ISave> _saves;
        private readonly NextFrameScheduler _saveScheduler;
        private readonly SaveSettings _settings;
        private bool _isWriting;
        private bool _hasPendingWrite;

        private string FilePath => Path.Combine(Application.persistentDataPath, _settings.FileName);

        public SaveService(
            IReadOnlyList<ISave> saves,
            SaveSettings settings)
        {
            _settings = settings;
            _saves = saves;
            _saveScheduler = new NextFrameScheduler(RequestWrite);

            foreach (var save in _saves)
            {
                save.DataUpdated += OnSaveDataUpdated;
            }
        }

        private void OnSaveDataUpdated()
        {
            _saveScheduler.Schedule();
        }

        public void PrepareDataAndSave()
        {
            _saveScheduler.Schedule();
        }

        private TSaveData ComposeData()
        {
            var data = new TSaveData();

            foreach (var save in _saves)
            {
                data.saves[save.SaveKey] = save.GetSave();
            }

            return data;
        }

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                {
                    IgnoreSerializableInterface = true,
                    IgnoreSerializableAttribute = true
                },
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new JsonConverter[]
                {
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter()
                }
            };
        }

        // One write at a time: a request that arrives during a write is
        // folded into a single follow-up write of the latest data.
        private void RequestWrite()
        {
            if (_isWriting)
            {
                _hasPendingWrite = true;
                return;
            }

            WriteLatestAsync().Forget();
        }

        private async UniTaskVoid WriteLatestAsync()
        {
            _isWriting = true;
            try
            {
                do
                {
                    _hasPendingWrite = false;
                    try
                    {
                        string json = JsonConvert.SerializeObject(ComposeData(), Formatting.None,
                            SerializerSettings);
                        await File.WriteAllTextAsync(FilePath, json);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error saving data: {e.Message}");
                    }
                } while (_hasPendingWrite);
            }
            finally
            {
                _isWriting = false;
            }
        }

        public async UniTask<TSaveData> LoadData()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Debug.LogWarning("Save file not found. Creating new save data.");
                    return new TSaveData();
                }

                string json = await File.ReadAllTextAsync(FilePath);
                var data = JsonConvert.DeserializeObject<TSaveData>(json, SerializerSettings);

                Debug.Log("Data loaded successfully.");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading data: {e.Message}");
                return new TSaveData();
            }
        }

        public async UniTask ResetSave()
        {
            try
            {
                await UniTask.WaitWhile(() => _isWriting);
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }

                foreach (var save in _saves)
                {
                    if (save is IResettableSave resettableSave)
                    {
                        resettableSave.ResetSave();
                    }
                }

                string json = JsonConvert.SerializeObject(ComposeData(), Formatting.None,
                    SerializerSettings);
                await File.WriteAllTextAsync(FilePath, json);
                Debug.Log($"Save reset successfully. Path: {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error resetting save: {e.Message}");
            }
        }
    }
}
