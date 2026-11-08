// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Services.Saves
{
    public interface ICheckpointSaveService<TSaveData> where TSaveData : class, ISaveData, new()
    {
        UniTask FlushAsync(CancellationToken cancellation = default);
        UniTask<TSaveData> LoadAsync(CancellationToken cancellation = default);
    }
}
