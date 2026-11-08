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
            TSaveData data = await _saveService.LoadData();

            foreach (var save in _saves)
            {
                save.Load(data.saves);
            }

            // foreach (var postSaveInitializable in _postSaveInitializables)
            // {
            //     postSaveInitializable.Initialize();
            // }
        }
    }
}
