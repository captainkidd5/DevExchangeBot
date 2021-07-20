using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExchangeBot.Commands;
using DevExchangeBot.Configuration;
using DevExchangeBot.Storage;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DevExchangeBot
{
    public static class Program
    {
        // TODO: The client class shouldn't be public, but there are
        // compatibility issues with the role menu system if we make
        // this private. This needs fixing.
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
            // Initialize a new client to establish a connection toe the Discord API.
            Client = new DiscordClient(new DiscordConfiguration
            {
                Token = Config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All // TODO: Enable intents in the bot's application page
            });

            Client.MessageCreated += ClientEvents.OnMessageCreatedLevelling;
            Client.MessageCreated += ClientEvents.OnMessageCreatedAutoQuoter;
            Client.GuildMemberRemoved += ClientEvents.OnGuildMemberRemoved;
            Client.MessageReactionAdded += ClientEvents.OnMessageReactionAdded;
            Client.MessageReactionRemoved += ClientEvents.OnMessageReactionRemoved;

            var commands = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = false,
                EnableMentionPrefix = false,
                StringPrefixes = new [] { Config.Prefix },
                IgnoreExtraArguments = true
            });

            //commands.CommandErrored += OnCommandErrored;

            //commands.RegisterCommands<LevellingCommands>();
            //commands.RegisterCommands<HeartboardCommands>();
            //commands.RegisterCommands<QuoterCommands>();
            commands.RegisterCommands<RoleMenuCommands>();

            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(30),
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteEmojis
            });

            var slash = Client.UseSlashCommands();

            slash.RegisterCommands<LevellingCommands>(802900619328225290); //TODO: Precise your own guildID here!
            slash.RegisterCommands<HeartboardCommands>(802900619328225290); //TODO: Precise your own guildID here!
            slash.RegisterCommands<QuoterCommands>(802900619328225290); //TODO: Precise your own guildID here!
            //slash.RegisterCommands<RoleMenuCommands>();

            slash.SlashCommandErrored += OnCommandErrored;

            StorageContext.InitializeStorage();

            await Client.ConnectAsync();
            await RoleMenuCommands.Initialize(Client);

            await Task.Delay(-1);
        }

        private static async Task OnCommandErrored(SlashCommandsExtension slashCommandsExtension, SlashCommandErrorEventArgs e)
        {
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
                    case CommandNotFoundException:
                        break; // Ignore

                    case SlashExecutionChecksFailedException cfe:
                    {
                        if (cfe.FailedChecks.Any(x => x is SlashRequireUserPermissions))
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
                    default:
                        embed = new DiscordEmbedBuilder
                        {
                            Title = "A problem occured while executing the command",
                            Description = $"{Config.Emoji.CriticalError} {Formatter.InlineCode(e.Context.CommandName)} threw an exception: `{ex?.GetType()}: {ex?.Message}`",
                            Color = new DiscordColor(0xFF0000)
                        };
                        break;
                }

                if (embed != null)
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .AddEmbed(embed).AsEphemeral(true));
        }
    }
}
