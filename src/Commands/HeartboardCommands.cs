using System.Threading.Tasks;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

// ReSharper disable UnusedMember.Global

namespace DevExchangeBot.Commands
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [SlashCommandGroup("heartboard", "Commands related to the heartboard module")]
    public class HeartboardCommands : SlashCommandModule
    {
        [SlashCommand("setchannel", "Sets the channel for the starboard")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task HbSetChannel(InteractionContext ctx,
            [Option("Channel", "Where will the heartboard messages go.")] DiscordChannel channel)
        {
            // Check if the given is a text channel, if not create a response to inform the user
            if (channel.Type != ChannelType.Text)
            {
                await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{Program.Config.Emoji.Failure} Oops, you need to select a text channel")
                        .AsEphemeral(true));
                return;
            }

            // Set the new value in the config
            StorageContext.Model.HeartBoardChannel = channel.Id;

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(Program.Config.Color),
                Description =
                    $"{Program.Config.Emoji.Success} Heartboard channel successfully set to {channel.Mention}!"
            };

            // Build a response (above) and proceed to send it to the user
            await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
        }

        [SlashCommand("toggle", "Toggles on or off the heartboard module")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task HbToggle(InteractionContext ctx,
            [Option("Enabled", "Whether to toggle on or off the module.")] bool enabled)
        {
            // Set the new value in the config
            StorageContext.Model.HeartBoardEnabled = enabled;

            // Build a response and send it
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor(Program.Config.Color),
                        Description =
                            $"{Program.Config.Emoji.Success} Heartboard successfully toggled to `{StorageContext.Model.HeartBoardEnabled}`!" +
                            $"{(StorageContext.Model.HeartBoardChannel == 0 ? $"\n{Program.Config.Emoji.Warning} No channel is set for the heartboard!" : null)}"
                    })
                    .AsEphemeral(true));
        }
    }
}
