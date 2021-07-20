using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [SlashCommandGroup("quoter", "Commands related to the quoter module")]
    public class QuoterCommands : SlashCommandModule
    {
        [SlashCommand("quote", "Quotes a message.")]
        public async Task Quote(InteractionContext ctx, [Option("Link", "Link to the message, only works in the current server.")] string link)
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

            var builder = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(Program.Config.Color),
                    Description = message.Content
                }
                .WithAuthor($"{message.Author.Username}#{message.Author.Discriminator}", iconUrl:message.Author.AvatarUrl)
                .AddField("Quoted by", $"{ctx.Member.Mention} from [#{message.Channel.Name}]({message.JumpLink})");

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(builder));
        }

        [SlashCommand("toggleautoquote", "Toggles on or off the auto-quoter"), SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task ToggleAutoQuote(InteractionContext ctx, [Option("Enabled", "Whether to enable the module")] bool enable)
        {
            StorageContext.Model.AutoQuoterEnabled = enable;

            var builder = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(Program.Config.Color),
                Description = $"{Program.Config.Emoji.Success} Auto-quoter toggled to `{StorageContext.Model.AutoQuoterEnabled}`!"
            };

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(builder).AsEphemeral(true));
        }
    }
}
