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
