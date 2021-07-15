using System.Collections.Generic;
using DevExchangeBot.Storage.Models;

namespace DevExchangeBot.Storage
{
    public class StorageModel
    {
        public Dictionary<ulong, UserModel> Users { get; set; }
        public float ExpMultiplier { get; set; }

        public ulong HeartBoardChannel { get; set; }
        public bool HeartBoardEnabled { get; set; }
        public IDictionary<ulong, ulong> HeartboardMessages { get; set; }

        public void AddUser(UserModel user)
        {
            Users.Add(user.Id, user);
        }
    }
}
