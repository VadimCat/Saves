// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

namespace Services.Saves
{
    public sealed class SaveSettings
    {
        public string FileName = "saveData.json";
        // Optional absolute path for isolated profiles/tests. Null uses persistentDataPath.
        public string DirectoryPath;
    }
}
