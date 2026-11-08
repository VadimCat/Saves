# Ji2 Saves

Extracted from `MergeMania/Assets/Scripts/Services/Saves` as an embedded package (`com.ji2.saves`).

This package follows the original flow:
- collect `ISave.GetSave()` into `saveData.saves`
- serialize to JSON
- deserialize JSON back
- pass `saveData.saves` to each `ISave.Load(...)`

`Dictionary<string, object>` values deserialize as `JObject` for complex objects, so existing
`((JObject)value).ToObject<MyType>()` patterns continue to work.

## Dependencies

- `com.cysharp.unitask`
- `com.unity.nuget.newtonsoft-json`
- `jp.hadashikick.vcontainer` (for installer/bootstrap assembly)

## Notes

- `SaveService<TSaveData>` requires `TSaveData : class, ISaveData, new()`.
- Call `ISaveService<TSaveData>.ResetSave()` to delete the save file, reset registered
  `IResettableSave` instances, and persist the reset state.
- VContainer helpers are in `Services.Saves.VContainer`:
  - `SaveInstaller.InstallSaves<TSaveData>(builder, settings)`
  - `SaveBootstrap<TSaveData>` (entry point)

## VContainer Wiring

Register in your scope:

```csharp
builder.InstallSaves<DefaultSaveData>(new SaveSettings { FileName = "saveData.json" });
```

If you use `SceneScope` + `MonoFeatureInstaller`, add `SaveMonoFeatureInstaller`
to the scope `installers` list.

If you need custom root data, use:

```csharp
public class MySaveData : ISaveData
{
    public Dictionary<string, object> saves { get; set; } = new();
    public int coins;
}

builder.InstallSaves<MySaveData>(new SaveSettings { FileName = "saveData.json" });
```

## Usage Example (Origin-Style)

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Services.Saves;

public sealed class GamesCountSave : ISave
{
    public string SaveKey => "games_count";
    public event Action DataUpdated;

    private int _count;

    public void Load(Dictionary<string, object> saveData)
    {
        if (saveData.TryGetValue(SaveKey, out var value) && value is JObject obj)
        {
            var dto = obj.ToObject<SaveDto>();
            _count = dto.Count;
        }
    }

    public object GetSave()
    {
        return new SaveDto { Count = _count };
    }

    public void Increment()
    {
        _count++;
        DataUpdated?.Invoke(); // triggers deferred save
    }

    private sealed class SaveDto
    {
        public int Count;
    }
}
```
