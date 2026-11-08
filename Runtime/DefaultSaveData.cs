// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

using System.Collections.Generic;

namespace Services.Saves
{
    public class DefaultSaveData : ISaveData
    {
        public Dictionary<string, object> saves { get; set; } = new();
    }
}
