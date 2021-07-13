using System.Collections.Generic;

namespace DevExchangeBot.Storage
{
    public class StorageModel
    {
        public Dictionary<string, ulong> Roles { get; set; }
        public ulong RoleMenuMsgID { get; set; }
        public ulong RoleMenuChannelID { get; set; }
    }
}
