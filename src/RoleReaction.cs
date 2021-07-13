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
        Dictionary<DiscordEmoji, DiscordRole> roles = new Dictionary<DiscordEmoji, DiscordRole>();

        [Command("addRole"), Description("Adds a role to Display in the Role Menu (ulong roleID, string _unicodeEmoji)")]
        public async Task AddRole(CommandContext _ctx, ulong _roleID, string _emojiName)
        {
            await _ctx.RespondAsync("Adding");

            DiscordRole role = _ctx.Guild.GetRole(_roleID);
            DiscordEmoji.TryFromUnicode(_ctx.Client, _emojiName, out DiscordEmoji emoji);

            roles.Add(emoji, role);
        }

        /// <summary>
        /// Creates A Role Menu in the channel which the command is Executed
        /// </summary>
        [Command("roleMenu")]
        public async Task CreateRoleMenu(CommandContext _ctx)
        {
            if (roles.Count == 0)
            {
                await _ctx.RespondAsync("You should add roles first...");
                return;
            }

            DiscordRole[] discordRoles = roles.Values.ToArray();

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<DiscordEmoji, DiscordRole> pair in roles)
            {
                sb.AppendLine($"{pair.Key} - {pair.Value.Name}");
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#0000FF"),
                Title = "Roles",
                Description = sb.ToString()
            };

            DiscordMessage msg = await _ctx.Message.RespondAsync(embed: embedBuilder.Build()).ConfigureAwait(false);

            foreach (DiscordEmoji emoji in roles.Keys)
            {
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

            roles.TryGetValue(_event.Emoji, out DiscordRole roleToGrant);

            DiscordMember member = (DiscordMember)_event.User;

            await member.GrantRoleAsync(roleToGrant);
            await _event.Message.DeleteReactionAsync(_event.Emoji, _event.User).ConfigureAwait(false);
        }
    }
}
