using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DSharpPlus.Entities;

#pragma warning disable 1998

namespace DevExchangeBot.Commands.Slash_Commands_Utilities
{
    /// <summary>
    /// This class and its method are used to provide Discord API the list of the available menus to spawn, edit or even delete.
    /// </summary>
    public class MenuNamesChoiceProvider
    {
        // ReSharper disable once UnusedMember.Global
        public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            return StorageContext.Model.RoleMenus.Select(menu => new DiscordApplicationCommandOptionChoice(menu.Name, menu.Name)).ToList();
        }
    }

    /// <summary>
    /// This one is used to provide selection types for role menus
    /// </summary>
    public class SelectionTypeChoiceProvider
    {
        // ReSharper disable once UnusedMember.Global
        public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            return new DiscordApplicationCommandOptionChoice[]
            {
                new("Single", "False"),
                new("Multiple", "True")
            };
        }
    }
}
