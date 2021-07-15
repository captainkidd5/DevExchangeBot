using System.Collections.Generic;
using DevExchangeBot.Storage.Models;

namespace DevExchangeBot.Storage
{
    public class StorageModel
    {
        public Dictionary<ulong, UserModel> Users { get; set; }
        public float ExpMultiplier { get; set; }

        public bool AutoQuoterEnabled { get; set; }
        public RoleMenuModel RoleMenu { get; set; }

        public StorageModel()
        {
            Users = new Dictionary<ulong, UserModel>();
            ExpMultiplier = 1;

            RoleMenu = new RoleMenuModel();
        }

        public void AddUser(UserModel user)
        {
            Users.Add(user.Id, user);
        }
    }
}
