// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

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
