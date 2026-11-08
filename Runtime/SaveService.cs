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
        private readonly IReadOnlyList<ISave> _saves;
        private readonly NextFrameScheduler _saveScheduler;
        private readonly SaveSettings _settings;

        private string FilePath => Path.Combine(Application.persistentDataPath, _settings.FileName);

        public SaveService(
            IReadOnlyList<ISave> saves,
            SaveSettings settings)
        {
            _settings = settings;
            _saves = saves;
            _saveScheduler = new NextFrameScheduler(() => Save(ComposeData()));

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

        private JsonSerializerSettings GetSerializerSettings()
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

        private async void Save(TSaveData data)
        {
            try
            {
                var settings = GetSerializerSettings();
                string json = JsonConvert.SerializeObject(data, Formatting.None, settings);
                await File.WriteAllTextAsync(FilePath, json);
                Debug.Log($"Data saved successfully. Path: {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving data: {e.Message}");
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
                var settings = GetSerializerSettings();
                var data = JsonConvert.DeserializeObject<TSaveData>(json, settings);

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

                var settings = GetSerializerSettings();
                string json = JsonConvert.SerializeObject(ComposeData(), Formatting.None, settings);
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
