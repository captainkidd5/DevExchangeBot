using System.Collections.Generic;
using DevExchangeBot.Storage.Models;

namespace DevExchangeBot.Storage
{
    public class StorageModel
    {
        public Dictionary<ulong, UserModel> Users { get; set; }
        public float ExpMultiplier { get; set; }

        public void AddUser(UserModel user)
        {
            Users.Add(user.Id, user);
        }

        public Dictionary<string, ulong> Roles { get; set; }
        public ulong RoleMenuMsgID { get; set; }
        public ulong RoleMenuChannelID { get; set; }
    }
}
