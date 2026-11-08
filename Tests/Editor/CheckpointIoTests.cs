using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Services.Saves;
using UnityEngine.TestTools;

namespace Ji2.Saves.Tests
{
    public sealed class CheckpointIoTests
    {
        private sealed class Section : ISave
        {
            public string SaveKey => "test";
            public int Value;
            public bool Flag;
            public int Loads;
            public event Action DataUpdated;
            public object GetSave() => JObject.FromObject(new { Value, Flag });
            public void Load(Dictionary<string, object> data) { Loads++; }
            public void Notify() => DataUpdated?.Invoke();
        }
        private string directory;
        private sealed class BootstrapService : ISaveService<DefaultSaveData>, ICheckpointSaveService<DefaultSaveData>
        {
            public readonly UniTaskCompletionSource<DefaultSaveData> Completion=new UniTaskCompletionSource<DefaultSaveData>();
            public int LegacyCalls,StrictCalls;
            public void PrepareDataAndSave() { }
            public UniTask ResetSave()=>UniTask.CompletedTask;
            public UniTask FlushAsync(CancellationToken cancellation=default)=>UniTask.CompletedTask;
            public UniTask<DefaultSaveData> LoadData(){LegacyCalls++;return Completion.Task;}
            public UniTask<DefaultSaveData> LoadAsync(CancellationToken cancellation=default)
            {StrictCalls++;return UniTask.FromException<DefaultSaveData>(new InvalidOperationException("Strict path is explicit"));}
        }
        private SaveService<DefaultSaveData> service;
        private Section section;
        private string FilePath => Path.Combine(directory, "fixture.json");
        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "Ji2SavesTests", Guid.NewGuid().ToString("N"));
            section = new Section();
            service = new SaveService<DefaultSaveData>(new ISave[] { section }, new SaveSettings
                { FileName = "fixture.json", DirectoryPath = directory });
        }
        [TearDown] public void TearDown()
        {
            service.Dispose();
            foreach (string name in new[] { "fixture.json", "fixture.json.tmp", "fixture.json.bak" })
                if (File.Exists(Path.Combine(directory, name))) File.Delete(Path.Combine(directory, name));
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
        [UnityTest] public IEnumerator AcknowledgedWritesPreserveZeroFalseAndLatestSnapshot() => UniTask.ToCoroutine(async () =>
        {
            section.Value = 17; section.Flag = true;
            var first = service.FlushAsync();
            section.Value = 0; section.Flag = false;
            var second = service.FlushAsync();
            await UniTask.WhenAll(first, second);
            var loaded = await service.LoadAsync();
            var json = (JObject)loaded.saves["test"];
            Assert.That((int)json["Value"], Is.Zero);
            Assert.That((bool)json["Flag"], Is.False);
            Assert.That(File.Exists(FilePath + ".bak"), Is.True);
            Assert.That(File.Exists(FilePath + ".tmp"), Is.False);
            var backup = JObject.Parse(File.ReadAllText(FilePath + ".bak"));
            Assert.That((int)backup["saves"]["test"]["Value"], Is.EqualTo(17));
        });
        [UnityTest] public IEnumerator CancelledFlushCannotReplacePreviousCheckpoint() => UniTask.ToCoroutine(async () =>
        {
            section.Value = 9; await service.FlushAsync();
            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel(); section.Value = 123;
                bool failed = false;
                try { await service.FlushAsync(cancelled.Token); } catch (OperationCanceledException) { failed = true; }
                Assert.That(failed, Is.True);
            }
            Assert.That((int)((JObject)(await service.LoadAsync()).saves["test"])["Value"], Is.EqualTo(9));
        });
        [UnityTest] public IEnumerator DisposalCancelsScheduledWriteAndUnsubscribes() => UniTask.ToCoroutine(async () =>
        {
            section.Notify(); service.Dispose(); section.Notify();
            await UniTask.Yield(); await UniTask.Yield();
            Assert.That(File.Exists(FilePath), Is.False);
            Assert.Throws<ObjectDisposedException>(() => service.PrepareDataAndSave());
        });
        [UnityTest] public IEnumerator StrictLoadRejectsCorruptionWithoutOverwritingIt() => UniTask.ToCoroutine(async () =>
        {
            Directory.CreateDirectory(directory); File.WriteAllText(FilePath, "broken checkpoint");
            bool failed = false;
            try { await service.LoadAsync(); } catch (Newtonsoft.Json.JsonException) { failed = true; }
            Assert.That(failed, Is.True);
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo("broken checkpoint"));
        });
        [UnityTest] public IEnumerator IoFailureReachesCaller() => UniTask.ToCoroutine(async () =>
        {
            Directory.CreateDirectory(directory); Directory.CreateDirectory(FilePath);
            bool failed = false;
            try { await service.FlushAsync(); } catch (IOException) { failed = true; }
            catch (UnauthorizedAccessException) { failed = true; }
            Assert.That(failed, Is.True);
            Directory.Delete(FilePath);
            Assert.That(File.Exists(FilePath + ".tmp"), Is.False);
        });
        [Test] public void RejectsDuplicateSectionsAndDirectoryTraversal()
        {
            Assert.Throws<ArgumentException>(() => new SaveService<DefaultSaveData>(new ISave[] { section, section },
                new SaveSettings { DirectoryPath = directory }));
            Assert.Throws<ArgumentException>(() => new SaveService<DefaultSaveData>(new ISave[] { section },
                new SaveSettings { FileName = "../other.json", DirectoryPath = directory }));
        }
        [UnityTest] public IEnumerator BootstrapPreservesExistingLoadPolicy() => UniTask.ToCoroutine(async () =>
        {
            var legacy=new BootstrapService();legacy.Completion.TrySetResult(new DefaultSaveData());
            var bootstrap=new Services.Saves.VContainer.SaveBootstrap<DefaultSaveData>(legacy,new ISave[]{section});
            await bootstrap.StartAsync(default);
            Assert.That(legacy.LegacyCalls,Is.EqualTo(1));Assert.That(legacy.StrictCalls,Is.Zero);Assert.That(section.Loads,Is.EqualTo(1));
        });
        [UnityTest] public IEnumerator CancelledBootstrapCannotApplyLateCompletion() => UniTask.ToCoroutine(async () =>
        {
            var legacy=new BootstrapService();
            var bootstrap=new Services.Saves.VContainer.SaveBootstrap<DefaultSaveData>(legacy,new ISave[]{section});
            using(var cancellation=new CancellationTokenSource())
            {
                var loading=bootstrap.StartAsync(cancellation.Token);cancellation.Cancel();bool cancelled=false;
                try{await loading;}catch(OperationCanceledException){cancelled=true;}
                Assert.That(cancelled,Is.True);legacy.Completion.TrySetResult(new DefaultSaveData());await UniTask.Yield();
                Assert.That(section.Loads,Is.Zero);
            }
        });
    }
}
