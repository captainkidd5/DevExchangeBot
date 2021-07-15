using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class HeartboardCommands : BaseCommandModule
    {
        [Command("hbsetchannel"), RequireUserPermissions(Permissions.Administrator), Description("Sets the channel for the starboard")]
        public async Task HbSetChannel(CommandContext ctx, [Description("Channel to post the messages in")] DiscordChannel channel)
        {
            StorageContext.Model.HeartBoardChannel = channel.Id;

            await ctx.RespondAsync(new DiscordEmbedBuilder
            {
                Color = new DiscordColor(Program.Config.Color),
                Description = $"{Program.Config.Emoji.Success} Heartboard channel successfully set to {channel.Mention}!"
            });
        }

        [Command("hbtoggle"), RequireUserPermissions(Permissions.Administrator), Description("Toggles the starboard module")]
        public async Task HbToggle(CommandContext ctx)
        {
            StorageContext.Model.HeartBoardEnabled = !StorageContext.Model.HeartBoardEnabled;

            await ctx.RespondAsync(new DiscordEmbedBuilder
            {
                Color = new DiscordColor(Program.Config.Color),
                Description =
                    $"{Program.Config.Emoji.Success} Heartboard successfully toggled to `{StorageContext.Model.HeartBoardEnabled}`!" +
                    $"{(StorageContext.Model.HeartBoardChannel == 0 ? $"\n{Program.Config.Emoji.Warning} No channel is set for the heartboard!" : null)}"
            });
        }
    }
}
