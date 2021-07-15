using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class QuoterCommands : BaseCommandModule
    {
        [Command("quote"), Description("Quotes a message.")]
        public async Task Quote(CommandContext ctx, [Description("Link to the message, only works in the current server.")] string link)
        {
            var match = Regex.Match(link, "^https://discord.com/channels/([0-9]+)/([0-9]+)/([0-9]+)$");

            if (!match.Success) return;

            if (!ulong.TryParse(match.Groups[1].Value, out var guildId) ||
                !ulong.TryParse(match.Groups[2].Value, out var channelId) ||
                !ulong.TryParse(match.Groups[3].Value, out var messageId)) return;

            DiscordMessage message;
            try
            {
                message = await (await ctx.Client.GetGuildAsync(guildId)).GetChannel(channelId).GetMessageAsync(messageId);
            }
            catch (Exception exception)
            {
                ctx.Client.Logger.LogWarning(exception, "Could not get message from the following link '{Link}'", link);
                return;
            }

            await ctx.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(Program.Config.Color),
                    Description = message.Content
                }
                .WithAuthor($"{message.Author.Username}#{message.Author.Discriminator}", iconUrl:message.Author.AvatarUrl)
                .AddField("Quoted by", $"{ctx.Member.Mention} from [#{message.Channel.Name}]({message.JumpLink})"));
        }

        [Command("toggleautoquote"), RequireUserPermissions(Permissions.Administrator), Description("Toggles on or off the auto-quoter")]
        public async Task ToggleAutoQuote(CommandContext ctx)
        {
            StorageContext.Model.AutoQuoterEnabled = !StorageContext.Model.AutoQuoterEnabled;

            await ctx.RespondAsync(new DiscordEmbedBuilder
            {
                Color = new DiscordColor(Program.Config.Color),
                Description = $"{Program.Config.Emoji.Success} Auto-quoter toggled to `{StorageContext.Model.AutoQuoterEnabled}`!"
            });
        }
    }
}
