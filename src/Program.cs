using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExchangeBot.Commands;
using DevExchangeBot.Configuration;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DevExchangeBot
{
    public static class Program
    {
        private static DiscordClient Client { get; set; }
        public static ConfigModel Config { get; private set; }

        private static void Main()
        {
            // Ensure the config.json file is copied to the output directory.
            if (!File.Exists("config.json"))
                throw new FileNotFoundException("Configuration file could not be found.", "config.json");

            // Parse the content of the configuration file and fire up the MainAsync method.
            Config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText("config.json"));
            MainAsync().GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            // Initialize a new client to establish a connection to the Discord API.
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });

            // Register all of the events
            Client.MessageCreated += ClientEvents.OnMessageCreatedLevelling;
            Client.MessageCreated += ClientEvents.OnMessageCreatedAutoQuoter;
            Client.GuildMemberRemoved += ClientEvents.OnGuildMemberRemoved;
            Client.MessageReactionAdded += ClientEvents.OnMessageReactionAdded;
            Client.MessageReactionRemoved += ClientEvents.OnMessageReactionRemoved;
            Client.ComponentInteractionCreated += ClientEvents.OnComponentInteractionCreatedRoleMenu;
            Client.ComponentInteractionCreated += ClientEvents.OnComponentInteractionCreatedRoleMenuSuppression;

            // Setup the interactivity
            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(30),
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteEmojis
            });

            // Setup the slash commands module
            var slash = Client.UseSlashCommands();

            if (Config.GuildId != 0)
            {
                // And register our commands if a guild ID is provided
                slash.RegisterCommands<LevellingCommands>(Config.GuildId);
                slash.RegisterCommands<HeartboardCommands>(Config.GuildId);
                slash.RegisterCommands<QuoterCommands>(Config.GuildId);
                slash.RegisterCommands<RoleMenuCommands>(Config.GuildId);
            }

            // Hook-up an event to see what's wrong if an error happen in a command
            slash.SlashCommandErrored += OnCommandErrored;

            // Initialize our storage
            StorageContext.InitializeStorage();

            // Then connect the bot to Discord's API
            await Client.ConnectAsync();

            PresenceUpdater.Initialize(Client);

            await Task.Delay(-1);
        }

        /// <summary>
        /// This method is very useful because it gives details if a command is errored and allow us to respond to the user
        /// </summary>
        private static async Task OnCommandErrored(SlashCommandsExtension slashCommandsExtension, SlashCommandErrorEventArgs e)
        {
            // First we log the error to the console
            e.Context.Client.Logger.LogError(new EventId(0, "Error"), e.Exception,
                "User '{Username}#{Discriminator}' ({UserId}) tried to execute '{Command}' "
                + "in #{ChannelName} ({ChannelId}) and failed with {ExceptionType}: {ExceptionMessage}",
                e.Context.User.Username, e.Context.User.Discriminator, e.Context.User.Id, e.Context.CommandName ?? "<unknown command>", e.Context.Channel.Name, e.Context.Channel.Id, e.Exception.GetType(), e.Exception.Message);

            DiscordEmbedBuilder embed = null;

            var ex = e.Exception;
            while (ex is AggregateException)
                ex = ex.InnerException;

            switch (ex)
            {
                // Then we check if the error is not user-related (e.g. wrong arguments, missing permission, etc...)
                case CommandNotFoundException:
                    break; // Ignore

                case SlashExecutionChecksFailedException cfe:
                {
                    if (cfe.FailedChecks.Any(x => x is SlashRequireUserPermissionsAttribute))
                        embed = new DiscordEmbedBuilder
                        {
                            Title = "Permission denied",
                            Description =
                                $"{Config.Emoji.AccessDenied} You lack permissions necessary to run this command.",
                            Color = new DiscordColor(0xFF0000)
                        };

                    break;
                }

                case ArgumentException:
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"{Config.Emoji.Failure} Oops, you used a wrong argument!")
                            .AsEphemeral(true));
                    break;

                // if it's none of those, we just provide a default embed with the error mentioned in it
                default:
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "A problem occured while executing the command",
                        Description = $"{Config.Emoji.CriticalError} {Formatter.InlineCode(e.Context.CommandName)} threw an exception: `{ex?.GetType()}: {ex?.Message}`",
                        Color = new DiscordColor(0xFF0000)
                    };
                    break;
            }

            // Finally we send our error message to the user
            if (embed != null)
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed).AsEphemeral(true));
        }
    }
}
