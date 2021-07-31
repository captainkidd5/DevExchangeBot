using System.Collections.Generic;
using DevExchangeBot.Storage.Models;

namespace DevExchangeBot.Storage
{
    /// <summary>
    /// This class contains data relative to the guild the bot is in, mainly users' data and modules' settings
    /// </summary>
    public class StorageModel
    {
        public Dictionary<ulong, UserModel> Users { get; set; }
        public float ExpMultiplier { get; set; }
        public bool EnableLevelUpChannel { get; set; }
        public ulong LevelUpChannelId { get; set; }

        public ulong HeartBoardChannel { get; set; }
        public bool HeartBoardEnabled { get; set; }
        public IDictionary<ulong, ulong> HeartboardMessages { get; set; }

        public bool AutoQuoterEnabled { get; set; }

        public IList<RoleMenuModel> RoleMenus { get; set; }

        public StorageModel()
        {
            Users = new Dictionary<ulong, UserModel>();
            ExpMultiplier = 1;

            HeartboardMessages = new Dictionary<ulong, ulong>();
            RoleMenus = new List<RoleMenuModel>();
        }

        public void AddUser(UserModel user)
        {
            Users.Add(user.Id, user);
        }
    }
}
