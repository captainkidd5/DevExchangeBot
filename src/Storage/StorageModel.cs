using System.Collections.Generic;
using DevExchangeBot.Models;

namespace DevExchangeBot.Storage
{
    public class StorageModel
    {
        public Dictionary<ulong, UserData> UserDictionary { get; set; }

        // EXP related settings
        public float XpMultiplier { get; set; }
    }
}
