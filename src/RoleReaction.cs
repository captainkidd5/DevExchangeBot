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
using DevExchangeBot.Storage;

namespace DevExchangeBot.RoleMenuSystem
{
    public class RoleReaction : BaseCommandModule
    {
        [Command("addRole"), Description("Adds a role to Display in the Role Menu (ulong roleID, string _unicodeEmoji)")]
        public async Task AddRole(CommandContext _ctx, ulong _roleID, string _emojiName)
        {
            DiscordRole role = _ctx.Guild.GetRole(_roleID);
            DiscordEmoji.TryFromUnicode(_ctx.Client, _emojiName, out DiscordEmoji emoji);

            if (StorageContext.Model == null) StorageContext.Model = new Storage.StorageModel();
            if (StorageContext.Model.Roles == null) StorageContext.Model.Roles = new Dictionary<string, ulong>();
            StorageContext.Model.Roles.Add(_emojiName, _roleID);

            DiscordChannel channel = await _ctx.Client.GetChannelAsync(StorageContext.Model.RoleMenuChannelID);
            await UpdateRoleMenu(await channel.GetMessageAsync(StorageContext.Model.RoleMenuMsgID));

            await _ctx.RespondAsync($"Added role: {role.Name} {emoji}");

        }

        /// <summary>
        /// Creates A Role Menu in the channel which the command is Executed
        /// </summary>
        [Command("roleMenu")]
        public async Task CreateRoleMenu(CommandContext _ctx)
        {
            if (StorageContext.Model.Roles?.Count == 0)
            {
                await _ctx.RespondAsync("You should add roles first...");
                return;
            }

            DiscordEmbed embed = CreateMenuEmbed(_ctx.Guild);

            DiscordMessage msg = await _ctx.Message.RespondAsync(embed: embed).ConfigureAwait(false);

            if (StorageContext.Model.Roles != null)
            {
                foreach (string emojiName in StorageContext.Model.Roles.Keys)
                {
                    DiscordEmoji.TryFromUnicode(emojiName, out DiscordEmoji emoji);
                    await msg.CreateReactionAsync(emoji);
                }
            }

            StorageContext.Model.RoleMenuMsgID = msg.Id;
            StorageContext.Model.RoleMenuChannelID = msg.ChannelId;
        }

        /// <summary>
        /// Assigns corrosponding Role to the User
        /// </summary>
        public static async Task OnReacted(DiscordClient _sender, MessageReactionAddEventArgs _event)
        {
            if (_event.User.IsBot && _event.Message.Id == StorageContext.Model.RoleMenuMsgID) return;

            StorageContext.Model.Roles.TryGetValue(_event.Emoji.Name, out ulong roleID);

            DiscordMember member = (DiscordMember)_event.User;

            await member.GrantRoleAsync(_event.Guild.GetRole(roleID)).ConfigureAwait(false);
            await _event.Message.DeleteReactionAsync(_event.Emoji, _event.User).ConfigureAwait(false);
        }

        private async Task UpdateRoleMenu(DiscordMessage _msg)
        {
            DiscordEmbed embed = CreateMenuEmbed(_msg.Channel.Guild);

            await _msg.ModifyAsync(embed).ConfigureAwait(false);

            await _msg.DeleteAllReactionsAsync();

            foreach (string emojiName in StorageContext.Model.Roles.Keys)
            {
                DiscordEmoji.TryFromUnicode(emojiName, out DiscordEmoji emoji);
                await _msg.CreateReactionAsync(emoji);
            }
        }

        private static DiscordEmbed CreateMenuEmbed(DiscordGuild _guild)
        {
            StringBuilder sb = new StringBuilder();

            List<DiscordRole> discordRoles = new List<DiscordRole>();

            if (StorageContext.Model.Roles != null)
            {
                foreach (KeyValuePair<string, ulong> pair in StorageContext.Model.Roles)
                {
                    DiscordRole role = _guild.GetRole(pair.Value);
                    discordRoles.Add(role);
                    sb.AppendLine($"{pair.Key} - {role.Name}");
                }
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#0000FF"),
                Title = "Roles",
                Description = sb.ToString()
            };

            return embedBuilder.Build();
        }

        public static void Initialize(DiscordClient _client)
        {
            if (StorageContext.Model.RoleMenuMsgID == 0 || StorageContext.Model.RoleMenuChannelID == 0) return;

            if (StorageContext.Model.Roles == null)
            {
                StorageContext.Model.Roles = new Dictionary<string, ulong>();
            }

            _client.MessageReactionAdded += OnReacted;
        }
    }
}
