// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer.Unity;

namespace Services.Saves.VContainer
{
    public class SaveBootstrap<TSaveData> : IAsyncStartable where TSaveData : class, ISaveData, new()
    {
        private readonly ISaveService<TSaveData> _saveService;
        private readonly IEnumerable<ISave> _saves;
        private readonly IReadOnlyCollection<IPostSaveInitializable> _postSaveInitializables;

        public SaveBootstrap(
            ISaveService<TSaveData> saveService,
            IEnumerable<ISave> saves
            // , IReadOnlyCollection<IPostSaveInitializable> postSaveInitializables
            )
        {
            _saveService = saveService;
            _saves = saves;
            // _postSaveInitializables = postSaveInitializables;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            // Preserve existing consumers' forgiving LoadData policy. Applications requiring
            // strict checkpoint rejection call ICheckpointSaveService.LoadAsync explicitly.
            TSaveData data = await _saveService.LoadData().AttachExternalCancellation(cancellation);
            cancellation.ThrowIfCancellationRequested();

            foreach (var save in _saves)
            {
                cancellation.ThrowIfCancellationRequested();
                save.Load(data.saves);
            }

            // foreach (var postSaveInitializable in _postSaveInitializables)
            // {
            //     postSaveInitializable.Initialize();
            // }
        }
    }
}
