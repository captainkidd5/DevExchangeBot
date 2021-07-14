using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DevExchangeBot.Storage.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace DevExchangeBot.Commands
{
    [Group("noodles")]
    [Description("Commands to interact with Noodles, the Dev Exchange mascot!")]
    public class NoodlesCommands : BaseCommandModule
    {
        [Command("status")]
        public async Task Status(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            var mood = StorageContext.Model.Noodles.GetOverallMood();

            var embed = new DiscordEmbedBuilder()
                .WithTitle(StorageContext.Model.Noodles.GetString(mood))
                .WithColor(new DiscordColor(Program.Config.Color));

            await ctx.RespondAsync(embed.Build());
        }
    }
}
