using System.Collections.Generic;

namespace Services.Saves
{
    public interface ISaveData
    {
        Dictionary<string, object> saves { get; set; }
    }
}
