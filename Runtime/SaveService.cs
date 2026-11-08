// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Services.Saves
{
    // Construct and call on the Unity thread. One owner per file; dispose with its scene scope.
    public class SaveService<TSaveData> : ISaveService<TSaveData>, ICheckpointSaveService<TSaveData>, IDisposable
        where TSaveData : class, ISaveData, new()
    {
        private readonly IReadOnlyList<ISave> _saves;
        private readonly NextFrameScheduler _saveScheduler;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly SemaphoreSlim _io = new SemaphoreSlim(1, 1);
        private readonly string _filePath;
        private long _requested, _persisted;
        private bool _disposed;

        public SaveService(IReadOnlyList<ISave> saves, SaveSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.FileName) || settings.FileName == "." || settings.FileName == ".." ||
                settings.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Path.GetFileName(settings.FileName) != settings.FileName)
                throw new ArgumentException("SaveSettings.FileName must be a file name.");
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            string directory = settings.DirectoryPath ?? Application.persistentDataPath;
            if (!Path.IsPathRooted(directory)) throw new ArgumentException("Save directory must be absolute.");
            _filePath = Path.Combine(directory, settings.FileName);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var save in _saves)
                if (save == null || string.IsNullOrEmpty(save.SaveKey) || !keys.Add(save.SaveKey))
                    throw new ArgumentException("Save sections require unique nonempty keys.");
            _saveScheduler = new NextFrameScheduler(SavePending, cancellationToken: _lifetime.Token);
            foreach (var save in _saves) save.DataUpdated += PrepareDataAndSave;
        }

        public void PrepareDataAndSave()
        {
            ThrowIfDisposed();
            _requested++;
            _saveScheduler.Schedule();
        }

        private void SavePending()
        {
            if (!_disposed && _requested > _persisted)
                WriteSnapshotAsync(_requested, _lifetime.Token).Forget(exception =>
                {
                    if (!(exception is OperationCanceledException)) Debug.LogException(exception);
                });
        }

        // Acknowledges this call's snapshot, never an unawaited async-void write.
        public UniTask FlushAsync(CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            return WriteSnapshotAsync(++_requested, cancellation);
        }

        private TSaveData ComposeData()
        {
            var data = new TSaveData();
            foreach (var save in _saves) data.saves.Add(save.SaveKey, save.GetSave());
            return data;
        }

        private static JsonSerializerSettings SerializerSettings() => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                IgnoreSerializableInterface = true, IgnoreSerializableAttribute = true
            },
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            Converters = new JsonConverter[] { new Vector3JsonConverter(), new QuaternionJsonConverter() }
        };

        private async UniTask WriteSnapshotAsync(long revision, CancellationToken cancellation)
        {
            // Freeze domain data before the first await; later mutations cannot change this write.
            string json = JsonConvert.SerializeObject(ComposeData(), Formatting.None, SerializerSettings());
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _lifetime.Token))
            {
                var token = linked.Token;
                await _io.WaitAsync(token).AsUniTask();
                string temporary = _filePath + ".tmp";
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (revision <= _persisted) return;
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                    await File.WriteAllTextAsync(temporary, json, token).AsUniTask();
                    token.ThrowIfCancellationRequested();
                    if (File.Exists(_filePath)) File.Replace(temporary, _filePath, _filePath + ".bak");
                    else File.Move(temporary, _filePath);
                    _persisted = revision;
                }
                finally
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    finally { _io.Release(); }
                }
            }
        }

        // Strict API: malformed/unreadable files propagate to the owning application.
        public async UniTask<TSaveData> LoadAsync(CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _lifetime.Token))
            {
                var token = linked.Token;
                await _io.WaitAsync(token).AsUniTask();
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (!File.Exists(_filePath)) return new TSaveData();
                    string json = await File.ReadAllTextAsync(_filePath, token).AsUniTask();
                    token.ThrowIfCancellationRequested();
                    var data = JsonConvert.DeserializeObject<TSaveData>(json, SerializerSettings());
                    if (data == null || data.saves == null) throw new InvalidDataException("Save has no section map.");
                    return data;
                }
                finally { _io.Release(); }
            }
        }

        // Preserve the original forgiving API for existing consumers.
        public async UniTask<TSaveData> LoadData()
        {
            try { return await LoadAsync(); }
            catch (OperationCanceledException) { throw; }
            catch (Exception exception) { Debug.LogError($"Error loading data: {exception.Message}"); return new TSaveData(); }
        }

        public async UniTask ResetSave()
        {
            ThrowIfDisposed();
            foreach (var save in _saves) if (save is IResettableSave resettable) resettable.ResetSave();
            await FlushAsync();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var save in _saves) save.DataUpdated -= PrepareDataAndSave;
            _lifetime.Cancel();
            // Pending operations own the semaphore until their finally blocks finish.
            // The scope and linked token sources become collectable after those continuations.
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }
    }
}
