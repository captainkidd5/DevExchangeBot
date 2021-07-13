using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext.Attributes;

namespace DevExchangeBot.src
{
    public class RoleReaction : BaseCommandModule
    {
        [Command("addRole"), Description("Adds a role to Display in the Role Menu (ulong roleID, string _unicodeEmoji)")]
        public async Task AddRole(CommandContext _ctx, ulong _roleID, string _emojiName)
        {
            DiscordRole role = _ctx.Guild.GetRole(_roleID);
            DiscordEmoji.TryFromUnicode(_ctx.Client, _emojiName, out DiscordEmoji emoji);

            if (Storage.StorageContext.Model == null) Storage.StorageContext.Model = new Storage.StorageModel();
            if (Storage.StorageContext.Model.Roles == null) Storage.StorageContext.Model.Roles = new Dictionary<string, ulong>();
            Storage.StorageContext.Model.Roles.Add(_emojiName, _roleID);

            await _ctx.RespondAsync($"Added role: {role.Name} {emoji}");
        }

        /// <summary>
        /// Creates A Role Menu in the channel which the command is Executed
        /// </summary>
        [Command("roleMenu")]
        public async Task CreateRoleMenu(CommandContext _ctx)
        {
            if (Storage.StorageContext.Model.Roles.Count == 0)
            {
                await _ctx.RespondAsync("You should add roles first...");
                return;
            }

            StringBuilder sb = new StringBuilder();

            List<DiscordRole> discordRoles = new List<DiscordRole>();

            foreach (KeyValuePair<string, ulong> pair in Storage.StorageContext.Model.Roles)
            {
                DiscordRole role = _ctx.Guild.GetRole(pair.Value);
                discordRoles.Add(role);
                sb.AppendLine($"{pair.Key} - {role.Name}");
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#0000FF"),
                Title = "Roles",
                Description = sb.ToString()
            };

            DiscordMessage msg = await _ctx.Message.RespondAsync(embed: embedBuilder.Build()).ConfigureAwait(false);

            foreach (string emojiName in Storage.StorageContext.Model.Roles.Keys)
            {
                DiscordEmoji.TryFromUnicode(emojiName, out DiscordEmoji emoji);
                await msg.CreateReactionAsync(emoji);
            }

            _ctx.Client.MessageReactionAdded += OnReacted;
        }

        /// <summary>
        /// Assigns corrosponding Role to the User
        /// </summary>
        private async Task OnReacted(DiscordClient _sender, MessageReactionAddEventArgs _event)
        {
            if (_event.User.IsBot) return;

            Storage.StorageContext.Model.Roles.TryGetValue(_event.Emoji.Name, out ulong roleID);

            DiscordMember member = (DiscordMember)_event.User;

            //await member.GrantRoleAsync(roleToGrant);
            await member.GrantRoleAsync(_event.Guild.GetRole(roleID));
            await _event.Message.DeleteReactionAsync(_event.Emoji, _event.User).ConfigureAwait(false);
        }
    }
}
