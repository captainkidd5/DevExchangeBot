using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace DevExchangeBot
{
    public static class ClientEvents
    {
        /// <summary>
        /// This method is responsible of handling the logic of the levelling module
        /// </summary>
        public static async Task OnMessageCreatedLevelling(DiscordClient sender, MessageCreateEventArgs e)
        {
            // If the user is a bot or the message starts with a prefix, cancel the action
            if (e.Author.IsBot)
                return;

            // Try to retrieve the data associated with the user, if fails, create it
            if (!StorageContext.Model.Users.TryGetValue(e.Author.Id, out var user))
            {
                user = new UserModel(e.Author.Id);
                StorageContext.Model.AddUser(user);
            }

            // Check if the user has already sent a message in the last minute, if yes, cancel the action
            if (user.LastMessageTime + TimeSpan.FromMinutes(1) > e.Message.CreationTimestamp.DateTime) return;

            var content = e.Message.Content;

            // Prevent the emoji strings to count for more than one character and add some EXP depending on the length of the message
            Regex.Replace(content, "<:[a-zA-Z]+:[0-9]+>", "0", RegexOptions.IgnoreCase);
            user.Exp += (int)Math.Round(content.Length / 2D * StorageContext.Model.ExpMultiplier, MidpointRounding.ToZero);

            // Reward the user with new level(s) if he can pass to the next level
            while (user.Exp > user.ExpToNextLevel)
            {
                user.Exp -= user.ExpToNextLevel;
                user.Level += 1;

                // The whole if clause below is dedicated to send a message on a level-up
                if (StorageContext.Model.LevelUpChannelId == 0 || StorageContext.Model.EnableLevelUpChannel == false)
                    await e.Channel.SendMessageAsync($"{Program.Config.Emoji.Confetti} {e.Author.Mention} advanced to level {user.Level}!");
                else
                {
                    DiscordChannel channel;

                    try
                    {
                        channel = e.Guild.GetChannel(StorageContext.Model.LevelUpChannelId);
                    }
                    catch (Exception exception)
                    {
                        sender.Logger.LogWarning(exception, "Could not get level-up channel of ID '{ChannelID}'",
                            StorageContext.Model.LevelUpChannelId);
                        await e.Channel.SendMessageAsync(
                            $"{Program.Config.Emoji.Confetti} {e.Author.Mention} advanced to level {user.Level}!");

                        user.LastMessageTime = e.Message.CreationTimestamp.DateTime;
                        return;
                    }

                    await channel.SendMessageAsync($"{Program.Config.Emoji.Confetti} {e.Author.Mention} advanced to level {user.Level}!");
                }
            }

            // Sets the last message time in the user's data as an anti-spam mesure
            user.LastMessageTime = e.Message.CreationTimestamp.DateTime;
        }

        /// <summary>
        /// This method handles the logic of the auto-quoter module
        /// </summary>
        public static async Task OnMessageCreatedAutoQuoter(DiscordClient sender, MessageCreateEventArgs e)
        {
            // Check if the auto-quoter is enabled and if the message sent contains only a link to a Discord message
            if (!StorageContext.Model.AutoQuoterEnabled) return;

            var match = Regex.Match(e.Message.Content, "^https://discord.com/channels/([0-9]+)/([0-9]+)/([0-9]+)$");
            if (!match.Success) return;

            // Try parsing the numbers in the link as ulongs, if one fails, the action is cancelled
            if (!ulong.TryParse(match.Groups[1].Value, out var guildId) ||
                !ulong.TryParse(match.Groups[2].Value, out var channelId) ||
                !ulong.TryParse(match.Groups[3].Value, out var messageId)) return;

            // Try getting the message
            DiscordMessage message;
            try
            {
                message = await (await sender.GetGuildAsync(guildId)).GetChannel(channelId).GetMessageAsync(messageId);
            }
            catch (Exception exception)
            {
                sender.Logger.LogWarning(exception, "Could not get message from the following link '{Link}'", e.Message.Content);
                return;
            }

            // If we got the message, we delete the original message to respond with a formatted quote
            await e.Message.DeleteAsync();
            await e.Channel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(Program.Config.Color),
                    Description = message.Content
                }
                .WithAuthor($"{message.Author.Username}#{message.Author.Discriminator}", iconUrl:message.Author.AvatarUrl)
                .AddField("Quoted by", $"{e.Message.Author.Mention} from [#{message.Channel.Name}]({message.JumpLink})"));
        }

        /// <summary>
        /// This method deletes a user's data when he leaves the guild in any way (leave, kick, ban, etc...)
        /// </summary>
        public static Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs guildMemberRemoveEventArgs)
        {
            StorageContext.Model.Users.Remove(guildMemberRemoveEventArgs.Member.Id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method contains the logic for the starboard
        /// </summary>
        public static async Task OnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            // If not enabled or no channel set, cancel
            if (!StorageContext.Model.HeartBoardEnabled || StorageContext.Model.HeartBoardChannel == 0) return;

            // Parse the emoji(s) specified in the global config
            var emojiList = Program.Config.RawHeartBoardEmojis.Select(configRawHeartBoardEmoji => ulong.TryParse(configRawHeartBoardEmoji, out var emojiId)
                    ? DiscordEmoji.FromGuildEmote(sender, emojiId)
                    : DiscordEmoji.FromName(sender, configRawHeartBoardEmoji))
                .ToList();

            // Try getting the orignal message, we need this because the event arguments do not provide everything we need
            // We also warp it in a try-catch because GetMessageAsync is a bit wonky
            DiscordMessage originalMessage;
            try
            {
                originalMessage = await e.Channel.GetMessageAsync(e.Message.Id);
            }
            catch (Exception exception)
            {
                sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not get the original message");
                return;
            }

            // Get all reactions associated with the specified emojis in the config, if there is any we get the total count of them
            var heartReactions = originalMessage.Reactions.Where(r => emojiList.Contains(r.Emoji)).ToList();

            if (!heartReactions.Any()) return;

            var reacNumber = heartReactions.Sum(discordReaction => discordReaction.Count);

            // Try to get the channel provided by the guild settings
            DiscordChannel channel;
            try
            {
                channel = e.Guild.GetChannel(StorageContext.Model.HeartBoardChannel);
            }
            catch (Exception exception)
            {
                sender.Logger.LogWarning(new EventId(1, "Warning"), exception, "Could not get starboard channel");
                return;
            }

            // Build a nice embed to display the message
            var embed = new DiscordEmbedBuilder
                {
                    Description = originalMessage.Content,
                    Color = new DiscordColor(reacNumber switch
                    {
                        >=25 => "#226383", >=10 => "#3c7f9e", >5 => "#5ca0bf", _ => "#8fc9e6",
                    })
                }
                .WithAuthor($"{originalMessage.Author.Username}", null, originalMessage.Author.AvatarUrl)
                .AddField("Original", $"[Click me!]({originalMessage.JumpLink})", true)
                .AddField("Stars",
                    $"{reacNumber switch {>=25 => ":sparkles:", >=10 => ":dizzy:", >5 => ":star2:", _ => ":star:"}} {reacNumber}", true)
                .WithFooter($"{originalMessage.CreationTimestamp.Date.ToLongDateString()}")
                .Build();

            StorageContext.Model.HeartboardMessages ??= new Dictionary<ulong, ulong>();

            // Check if the message has already been posted, if yes, update it, if no, create it
            if (StorageContext.Model.HeartboardMessages.TryGetValue(originalMessage.Id, out var starboardMessage))
            {
                try
                {
                    var message = await channel.GetMessageAsync(starboardMessage);
                    await message.ModifyAsync(embed);
                }
                catch (Exception exception)
                {
                    sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not update starboard message");
                }
                return;
            }

            if (reacNumber < Program.Config.HeartboardRequirement) return;

            var hbMessage = await channel.SendMessageAsync(embed);

            // Register the new message in the storage
            StorageContext.Model.HeartboardMessages.Add(originalMessage.Id, hbMessage.Id);
        }

        /// <summary>
        /// This method is identical to <see cref="OnMessageReactionAdded"/>, except it cannot send messages, only update them
        /// </summary>
        public static async Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            if (!StorageContext.Model.HeartBoardEnabled || StorageContext.Model.HeartBoardChannel == 0) return;

            var emojiList = Program.Config.RawHeartBoardEmojis.Select(configRawHeartBoardEmoji => ulong.TryParse(configRawHeartBoardEmoji, out var emojiId)
                    ? DiscordEmoji.FromGuildEmote(sender, emojiId)
                    : DiscordEmoji.FromName(sender, configRawHeartBoardEmoji))
                .ToList();

            DiscordMessage originalMessage;
            try
            {
                originalMessage = await e.Channel.GetMessageAsync(e.Message.Id);
            }
            catch (Exception exception)
            {
                sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not get the original message");
                return;
            }

            var heartReactions = originalMessage.Reactions.Where(r => emojiList.Contains(r.Emoji)).ToList();

            var reacNumber = heartReactions.Sum(discordReaction => discordReaction.Count);

            DiscordChannel channel;
            try
            {
                channel = e.Guild.GetChannel(StorageContext.Model.HeartBoardChannel);
            }
            catch (Exception exception)
            {
                sender.Logger.LogWarning(new EventId(1, "Warning"), exception, "Could not get starboard channel");
                return;
            }

            var embed = new DiscordEmbedBuilder
                {
                    Description = originalMessage.Content,
                    Color = new DiscordColor(reacNumber switch
                    {
                        >=25 => "#226383", >=10 => "#3c7f9e", >5 => "#5ca0bf", _ => "#8fc9e6",
                    })
                }
                .WithAuthor($"{originalMessage.Author.Username}", null, originalMessage.Author.AvatarUrl)
                .AddField("Original", $"[Click me!]({originalMessage.JumpLink})", true)
                .AddField("Stars",
                    $"{reacNumber switch {>=25 => ":sparkles:", >=10 => ":dizzy:", >5 => ":star2:", _ => ":star:"}} {reacNumber}", true)
                .WithFooter($"{originalMessage.CreationTimestamp.Date.ToLongDateString()}")
                .Build();

            StorageContext.Model.HeartboardMessages ??= new Dictionary<ulong, ulong>();

            if (StorageContext.Model.HeartboardMessages.TryGetValue(originalMessage.Id, out var starboardMessage))
            {
                try
                {
                    var message = await channel.GetMessageAsync(starboardMessage);
                    await message.ModifyAsync(embed);
                }
                catch (Exception exception)
                {
                    sender.Logger.LogError(new EventId(0, "Error"), exception, "Could not update starboard message");
                }
            }
        }

        public static Task OnComponentInteractionCreatedRoleMenu(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            // Check if the component we interacted with is a role menu
            var match = Regex.Match(e.Id, "^roleMenu_(.+$)");
            if (!match.Success) return Task.CompletedTask;

            // We'll run everything in a task to avoid deadlocks and failed interactions
            _ = Task.Run(async () =>
            {
                // Get the menu name and try to retrieve it from the storage
                var menuName = match.Groups[1].Value;

                if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
                {
                    sender.Logger.LogWarning("Could not get menu of name '{MenuName}', data may have been altered", menuName);
                    return;
                }

                var menu = StorageContext.Model.RoleMenus.First(m => m.Name == menuName);

                // Convert the DiscordUser to a DiscordMember, this will give us access to methods to modify it's roles
                var member = await e.Guild.GetMemberAsync(e.User.Id);

                // Loop through all of the roles the user has not selected and revoke them
                foreach (var option in menu.Options.Where(o => !e.Values.Contains(o.RoleId.ToString())))
                {
                    try
                    {
                        var role = e.Guild.GetRole(option.RoleId);
                        await member.RevokeRoleAsync(role);
                    }
                    catch (Exception exception)
                    {
                        sender.Logger.LogWarning(exception, "Could not either get role of ID '{RoleId}' or revoke the previous role from user '{Username}#{Tag}' ({UserID})",
                            option.RoleId, member.Username, member.Discriminator, member.Id);
                    }
                }

                // Loop through all of the roles the user has selected and grant them
                foreach (var value in e.Values)
                {
                    try
                    {
                        var role = e.Guild.GetRole(ulong.Parse(value));
                        await member.GrantRoleAsync(role);
                    }
                    catch (Exception exception)
                    {
                        sender.Logger.LogWarning(exception, "Could not either get role of ID '{RoleId}' or grant the previous role from user '{Username}#{Tag}' ({UserID})",
                            value, member.Username, member.Discriminator, member.Id);
                    }
                }

                // Finally send a response ton indicate the user his roles have been updated
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Roles updated").AsEphemeral(true));
            });
            return Task.CompletedTask;
        }

        public static async Task OnComponentInteractionCreatedRoleMenuSuppression(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            // Check if the component is a delete button
            var match = Regex.Match(e.Id, "^deleteMenu_(.+$)");

            // If not, check if it's a cancel button
            if (e.Id == "cancelMenuSuppression" && !match.Success)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Success} Action cancelled, nothing has been erased!")
                        .AsEphemeral(true));
                return;
            }

            // If none of them, cancel
            if (!match.Success) return;

            // Again, run the event in a task to avoid deadlocks
            _ = Task.Run(async () =>
            {
                // Get the menu if it exists and delete it
                var menuName = match.Groups[1].Value;

                if (StorageContext.Model.RoleMenus.All(m => m.Name != menuName))
                {
                    sender.Logger.LogWarning("Could not get menu of name '{MenuName}', data may have been altered", menuName);
                    return;
                }

                StorageContext.Model.RoleMenus.Remove(StorageContext.Model.RoleMenus.First(m => m.Name == menuName));

                // Send a response to indicate a successful deletion
                await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Success} Menu successfully deleted!")
                        .AsEphemeral(true));
            });
        }
    }
}
