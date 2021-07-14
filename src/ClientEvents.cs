using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace DevExchangeBot
{
    public static class ClientEvents
    {
        public static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
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

        public static Task OnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs guildMemberRemoveEventArgs)
        {
            StorageContext.Model.Users.Remove(guildMemberRemoveEventArgs.Member.Id);
            return Task.CompletedTask;
        }
    }
}
