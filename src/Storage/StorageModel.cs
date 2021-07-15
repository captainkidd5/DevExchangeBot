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

        public RoleMenuModel RoleMenu { get; set; }

        public StorageModel()
        {
            Users = new Dictionary<ulong, UserModel>();
            ExpMultiplier = 1;
          
            HeartboardMessages = new Dictionary<ulong, ulong>();
            RoleMenu = new RoleMenuModel();
        }


        public void AddUser(UserModel user)
        {
            Users.Add(user.Id, user);
        }
    }
}
