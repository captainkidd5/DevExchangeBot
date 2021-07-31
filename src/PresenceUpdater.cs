using System;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;

namespace DevExchangeBot
{
    public static class PresenceUpdater
    {
        private static DiscordClient _client;
        private static Timer _timer;
        private static DateTime _startupTime;

        public static async void Initialize(DiscordClient client)
        {
            _client = client;

            _startupTime = DateTime.UtcNow;

            _timer = new Timer
            {
                AutoReset = true,
                Enabled = false,
                Interval = 60_000 * 10
            };
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();

            await Task.Delay(1000);
            var guild = await _client.GetGuildAsync(Program.Config.GuildId, true);
            await _client.UpdateStatusAsync(
                new DiscordActivity(
                    $"over {guild.MemberCount} users",
                    ActivityType.Watching),
                UserStatus.Online, _startupTime);
        }

        private static async void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var guild = await _client.GetGuildAsync(Program.Config.GuildId, true);
            await _client.UpdateStatusAsync(
                new DiscordActivity(
                    $"over {guild.MemberCount} users",
                    ActivityType.Watching),
                UserStatus.Online, _startupTime);
        }
    }
}
