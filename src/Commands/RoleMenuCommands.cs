using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DevExchangeBot.Storage;

namespace DevExchangeBot.Commands
{
    [Group("rolemenu")]
    public class RoleMenuCommands : BaseCommandModule
    {
        [Command("addrole"), Aliases("a")]
        [Description("Adds a role to the menu")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task AddRole(CommandContext ctx, DiscordRole role, DiscordEmoji emoji)
        {
            // Add the role and emoji to the list
            bool success = StorageContext.Model.RoleMenu.AddRole(role, emoji);
            await ctx.TriggerTypingAsync();

            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(Program.Config.Color))
                .WithFooter("This message will automatically disappear in 5 seconds!");

            if (success)
            {
                var channel = await ctx.Client.GetChannelAsync(StorageContext.Model.RoleMenu.ChannelId);
                await UpdateRoleMenu(await channel.GetMessageAsync(StorageContext.Model.RoleMenu.MessageId));

                embed.WithDescription($"{Program.Config.Emoji.Success} Added role: **{role.Name}** {emoji}");
            }
            else
            {
                embed
                    .WithDescription($"{Program.Config.Emoji.Failure} Failed to add role: **{role.Name}** {emoji}!")
                    .WithColor(new DiscordColor(255, 0, 0));
            }

            var message = await ctx.RespondAsync(embed.Build());
            await Task.Delay(5000);

            await ctx.Message.DeleteAsync();
            await message.DeleteAsync();
        }

        [Command("create"), Aliases("c")]
        [Description("Creates a role menu in the current channel")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task Create(CommandContext ctx)
        {
            var embed = CreateMenuEmbed(ctx.Guild);
            var message = await ctx.Channel.SendMessageAsync(embed);

            foreach (DiscordEmoji emoji in StorageContext.Model.RoleMenu.GetAllEmojis())
                await message.CreateReactionAsync(emoji);

            StorageContext.Model.RoleMenu.MessageId = message.Id;
            StorageContext.Model.RoleMenu.ChannelId = message.ChannelId;

            await ctx.Message.DeleteAsync();
        }

        // Just in case for some reason you want to switch the Role Menu
        // without creating a new one
        [Command("setmessage"), Aliases("m")]
        [Description("Changes on which message the role menu should display")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetMessage(CommandContext ctx, DiscordChannel channel, ulong messageId)
        {
            // Get the message
            var message = await channel.GetMessageAsync(messageId);

            // Assign the new message id and channel id
            StorageContext.Model.RoleMenu.MessageId = message.Id;
            StorageContext.Model.RoleMenu.ChannelId = message.ChannelId;

            // Update Role Menu on the new message
            await UpdateRoleMenu(message);
        }

        private static async Task OnReacted(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            if (StorageContext.Model.RoleMenu.MessageId == 0 || StorageContext.Model.RoleMenu.ChannelId == 0)
                return; // Role Menu Not Created

            if (e.User.IsBot || e.Message.Id != StorageContext.Model.RoleMenu.MessageId)
                return; // Reaction Not On Role Menu

            // Make sure the Role exists
            if (!StorageContext.Model.RoleMenu.GetRoleId(e.Emoji, out ulong _roleID))
            {
                await e.Message.DeleteReactionsEmojiAsync(e.Emoji);
                return;
            }

            var member = (DiscordMember)e.User;

            // Grant the Role and delete the Reaction
            await member.GrantRoleAsync(e.Guild.GetRole(_roleID)).ConfigureAwait(false);
            await e.Message.DeleteReactionAsync(e.Emoji, e.User).ConfigureAwait(false);
        }

        private static async Task UpdateRoleMenu(DiscordMessage message)
        {
            // Update the message
            var embed = CreateMenuEmbed(message.Channel.Guild);
            await message.ModifyAsync(embed).ConfigureAwait(false);

            // Set up reactions again
            await message.DeleteAllReactionsAsync();

            // Add Role Reactions
            foreach (DiscordEmoji emoji in StorageContext.Model.RoleMenu.GetAllEmojis())
                await message.CreateReactionAsync(emoji);
        }

        private static DiscordEmbed CreateMenuEmbed(DiscordGuild guild)
        {
            // Build the Description
            var builder = new StringBuilder();
            builder.AppendLine(Program.Config.RoleMenu.Description);

            // Build the Roles and Emojis
            foreach (DiscordEmoji emoji in StorageContext.Model.RoleMenu.GetAllEmojis())
            {
                // Make sure the Role exists then add it onto the description
                if (!StorageContext.Model.RoleMenu.GetRoleId(emoji, out ulong roleId))
                    continue;

                var role = guild.GetRole(roleId);
                builder.AppendLine($"{emoji} - {role.Name}");
            }

            // Wrap it up in a nice Embed
            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(Program.Config.Color))
                .WithTitle(Program.Config.RoleMenu.Title)
                .WithDescription(builder.ToString());

            return embed.Build();
        }

        public static async Task Initialize(DiscordClient client)
        {
            // If the config exists then check if anyone reacted while the bot was offline
            if (StorageContext.Model.RoleMenu.ChannelId != 0)
            {
                // Get the Role Menu Message
                var channel = await client.GetChannelAsync(StorageContext.Model.RoleMenu.ChannelId);
                var message = await channel.GetMessageAsync(StorageContext.Model.RoleMenu.MessageId);

                if (message.Channel == null)
                {
                    // I don't know why. I shouldn't have to wonder why.
                    // But for some reason randomly the GetMessageAsync
                    // returns a message that doesn't have a channel
                    // So we have to skip checking for reactions
                    client.MessageReactionAdded += OnReacted;
                    return;
                }

                // Get all reactions
                foreach (DiscordReaction reaction in message.Reactions)
                {
                    if (reaction.Count == 1)
                        continue;

                    if (StorageContext.Model.RoleMenu.GetRoleId(reaction.Emoji, out ulong roleId))
                    {
                        var users = await message.GetReactionsAsync(reaction.Emoji);

                        // Get users who reacted with the emoji
                        foreach (DiscordUser user in users)
                        {
                            if (user.IsBot)
                                continue;

                            // Grant role to user
                            DiscordMember member = await channel.Guild.GetMemberAsync(user.Id);
                            await member.GrantRoleAsync(member.Guild.GetRole(roleId));
                        }
                    }
                }

                // In case someone reacted with an emoji outside of our list
                // refresh the message to remove it
                await UpdateRoleMenu(message);
            }

            // Listen to reactions on the Role Menu
            client.MessageReactionAdded += OnReacted;
        }
    }
}
