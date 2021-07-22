using System.Collections.Generic;

namespace DevExchangeBot.Storage.Models
{
    public class RoleMenuModel
    {
        public string Name { get; set; }

        public bool AllowMultipleSelection { get; set; }

        public IList<RoleOption> Options { get; set; }
    }

    public class RoleOption
    {
        public ulong RoleId { get; set; }
        public string Description { get; set; }
        public string Emoji { get; set; }
    }
}
