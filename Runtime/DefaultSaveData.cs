using System.Collections.Generic;

namespace Services.Saves
{
    public class DefaultSaveData : ISaveData
    {
        public Dictionary<string, object> saves { get; set; } = new();
    }
}
