using System;
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
        public static async Task OnMessageCreatedLevelling(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Message.Content.StartsWith(Program.Config.Prefix))
                return;

            if (!StorageContext.Model.Users.TryGetValue(e.Author.Id, out var user))
            {
                user = new UserModel(e.Author.Id);
                StorageContext.Model.AddUser(user);
            }

            var content = e.Message.Content;

            Regex.Replace(content, ":[0-9a-z]+:", "0", RegexOptions.IgnoreCase);
            user.Exp += (int)Math.Round(content.Length / 2D * StorageContext.Model.ExpMultiplier, MidpointRounding.ToZero);

            if (user.Exp > user.ExpToNextLevel)
            {
                user.Exp -= user.ExpToNextLevel;
                user.Level += 1;

                await e.Channel.SendMessageAsync($"{Program.Config.Emoji.Confetti} {e.Author.Mention} advanced to level {user.Level}!");
            }
        }

        public static async Task OnMessageCreatedAutoQuoter(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!StorageContext.Model.AutoQuoterEnabled) return;

            var match = Regex.Match(e.Message.Content, "^https://discord.com/channels/([0-9]+)/([0-9]+)/([0-9]+)$");

            if (!match.Success) return;

            if (!ulong.TryParse(match.Groups[1].Value, out var guildId) ||
                !ulong.TryParse(match.Groups[2].Value, out var channelId) ||
                !ulong.TryParse(match.Groups[3].Value, out var messageId)) return;

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

            await e.Message.DeleteAsync();
            await e.Channel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(Program.Config.Color),
                    Description = message.Content
                }
                .WithAuthor($"{message.Author.Username}#{message.Author.Discriminator}", iconUrl:message.Author.AvatarUrl)
                .AddField("Quoted by", $"{e.Message.Author.Mention} from [#{message.Channel.Name}]({message.JumpLink})"));
        }

        public static Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs guildMemberRemoveEventArgs)
        {
            StorageContext.Model.Users.Remove(guildMemberRemoveEventArgs.Member.Id);
            return Task.CompletedTask;
        }
    }
}
