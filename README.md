# Ji2 Saves

Shared JSON persistence copied from TapAway commit `1f9978463c26606dd2b51dba35ff0b3acdeb2081`, originally extracted from `MergeMania/Assets/Scripts/Services/Saves`. Runtime source and copyright headers are retained. This consumer adds reusable Editor reset behavior without dialogs and registration of additional legacy file names.

## Runtime integration

Dependencies: UniTask, Newtonsoft Json and VContainer for the bootstrap assembly.

```csharp
builder.Register<MyProgressSave>(Lifetime.Singleton).As<ISave>();
builder.RegisterInstance(new SaveSettings { FileName = "saveData.json" });
builder.Register<SaveService<DefaultSaveData>>(Lifetime.Singleton)
    .As<ISaveService<DefaultSaveData>>();
builder.Register<SaveBootstrap<DefaultSaveData>>(Lifetime.Singleton);
```

In the application's `IAsyncStartable` initialization pipeline, await `SaveBootstrap<DefaultSaveData>.StartAsync(cancellation)` before starting gameplay or presenting progress-dependent UI. The package does not provide an `InstallSaves` extension.

Each `ISave` has a unique `SaveKey`, returns its data from `GetSave()`, loads its section from `Dictionary<string, object>`, and raises `DataUpdated` after changes. The service collects sections in `DefaultSaveData.saves`, schedules one write on the next player-loop frame, and serializes JSON. Complex values load as `JObject`. Implement `IResettableSave.ResetSave()` to restore defaults when the runtime service resets.

The serializer uses `DefaultValueHandling.IgnoreAndPopulate`. A DTO with constructor/field defaults can return an explicit `JObject.FromObject(dto)` from `GetSave()` to preserve false and zero values exactly. The Snowball adapter demonstrates this and legacy migration in `Assets/SnowballSweep/Runtime/ProgressStore.cs`.

Version 1.1 adds `ICheckpointSaveService<T>.FlushAsync(cancellation)` and `LoadAsync(cancellation)` without changing the original interface. Flush freezes the current sections before awaiting, serializes writes per service, and completes after file replacement. A cancelled or failed operation throws; acknowledge saves only after awaiting it. Strict load propagates corruption/I/O errors; legacy `LoadData` retains its logged fallback. Missing files return empty sections. The application validates versioned content before applying it.

Writes use a same-directory `.tmp`, then `File.Replace` with the previous primary as `.bak` (or move for the first write). Replacement failure propagates; the primary is never deleted first. Backup recovery is an explicit application decision. This is local persistence, not cloud sync or a power-loss durability guarantee. Support for atomic replacement must be verified on each target filesystem.

Use one service owner per file, call it on Unity's main thread, register/dispose it with its scene scope, and await save operations before leaving that scope. Dispose unsubscribes sections and cancels queued/in-flight operations. `SaveSettings.DirectoryPath` optionally supplies an absolute profile/test directory; null retains `Application.persistentDataPath`. Reset now uses the same serialized writer. VContainer bootstrap observes cancellation before applying sections. Explicit flush captures this call's snapshot; mutations after it require a later flush or `DataUpdated` event.

Package tests use unique temporary fixture directories and never touch real player progress.

The existing VContainer bootstrap retains the forgiving `LoadData` contract while checking cancellation before applying sections. A strict application coordinator calls `LoadAsync` explicitly. Compatibility and cancellation tests cover bootstrap completion after its owner has cancelled.

## Editor commands

- **Game > Save > Reset Save**: immediate reset, no confirmation or completion dialog. Stops the current Editor's PlayMode before removing files; a SessionState flag preserves the pending reset across domain reload. Logs success/errors to the Console.
- **Game > Save > Open Save Folder**.
- **Game > Save > Print Save Path**.

The reset removes `saveData.json`, its `.bak`/`.tmp`, and this application's PlayerPrefs. A game can register additional file names from an `[InitializeOnLoad]` Editor adapter:

```csharp
SaveTools.RegisterSaveFile("old-progress.json");
```

Only file names are accepted, never directory paths. Registered legacy files and their `.bak`/`.tmp` variants are included; unrelated files remain intact. `DeleteSaveFiles(directory)` allows consumer tests to exercise cleanup in a temporary fixture directory without deleting the real player save.
