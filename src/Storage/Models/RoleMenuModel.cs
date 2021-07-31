using System.Collections.Generic;

namespace DevExchangeBot.Storage.Models
{
    /// <summary>
    /// This class represent a role menu itself.
    /// </summary>
    public class RoleMenuModel
    {
        public string Name { get; set; }

        public bool AllowMultipleSelection { get; set; }

        public IList<RoleOption> Options { get; set; }
    }

    /// <summary>
    /// This class represent an option in a role menu.
    /// See <see cref="RoleMenuModel"/>.
    /// </summary>
    public class RoleOption
    {
        public ulong RoleId { get; set; }
        public string Description { get; set; }
        public string Emoji { get; set; }
    }
}
