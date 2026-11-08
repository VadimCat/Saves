using System;
using System.Collections.Generic;

namespace Services.Saves
{
    public interface ISave
    {
        string SaveKey { get; }
        void Load(Dictionary<string, object> saveData);
        object GetSave();
        event Action DataUpdated;
    }

    public interface IResettableSave
    {
        void ResetSave();
    }
}
