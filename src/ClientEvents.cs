using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Configuration;
using DevExchangeBot.Models;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace DevExchangeBot
{
    public static class ClientEvents
    {
        public static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Message.Content.StartsWith("dx!")) return; // TODO: Change prefix here if you changed it in the configuration

            if (!StorageContext.Model.UserDictionary.TryGetValue(e.Author.Id, out var usrData))
            {
                usrData = new UserData(e.Author.Id);
                StorageContext.Model.UserDictionary.Add(e.Author.Id, usrData);
            }

            var message = e.Message.Content;

            Regex.Replace(message, ":[0-9a-z]+:", "0", RegexOptions.IgnoreCase);

            var xpToAdd = (int) Math.Round(message.Length / 2D * StorageContext.Model.XpMultiplier, MidpointRounding.ToZero);

            usrData.Xp += xpToAdd;

            if (usrData.Xp > usrData.XpToNextLevel)
            {
                usrData.Xp -= usrData.XpToNextLevel;
                usrData.Level += 1;

                await e.Channel.SendMessageAsync($"{Emojis.Confetti} {e.Author.Mention} advanced to level {usrData.Level}!");
            }
        }
    }
}
