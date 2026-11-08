using Cysharp.Threading.Tasks;

namespace Services.Saves
{
    public interface ISaveService<TSaveData> where TSaveData : class, ISaveData, new()
    {
        void PrepareDataAndSave();
        UniTask<TSaveData> LoadData();
        UniTask ResetSave();
    }
}
