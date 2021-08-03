using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace DevExchangeBot.Configuration
{
    /// <summary>
    /// This class has only one method used to setup the custom log factory
    /// </summary>
    public static class Logging
    {
        public static ILoggerFactory SetUpAndGetLoggerFactory()
        {
            const string logTemplate = "[{Timestamp:HH:mm:ss} | {Level:u3}] {Message:lj} {Exception:j}{NewLine}";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: logTemplate, theme: new LoggingTheme())
                .WriteTo.File("logs/logs-.log", outputTemplate: logTemplate,
                    rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            return new LoggerFactory().AddSerilog();
        }
    }

    /// <summary>
    /// This class is used to set a custom them to the console and log messages
    /// </summary>
    public sealed class LoggingTheme : ConsoleTheme
    {
        public override bool CanBuffer => false;

        protected override int ResetCharCount => 0;

        public override void Reset(TextWriter output)
        {
            Console.ResetColor();
        }

        public override int Set(TextWriter output, ConsoleThemeStyle style)
        {
            (ConsoleColor foreground, ConsoleColor background) = style switch
            {
                ConsoleThemeStyle.Scalar => (ConsoleColor.Green, ConsoleColor.Black),
                ConsoleThemeStyle.Number => (ConsoleColor.DarkGreen, ConsoleColor.Black),
                ConsoleThemeStyle.LevelDebug => (ConsoleColor.DarkMagenta, ConsoleColor.Black),
                ConsoleThemeStyle.LevelError => (ConsoleColor.Red, ConsoleColor.Black),
                ConsoleThemeStyle.LevelFatal => (ConsoleColor.DarkRed, ConsoleColor.Black),
                ConsoleThemeStyle.LevelVerbose => (ConsoleColor.Magenta, ConsoleColor.Black),
                ConsoleThemeStyle.LevelWarning => (ConsoleColor.Yellow, ConsoleColor.Black),
                ConsoleThemeStyle.SecondaryText => (ConsoleColor.DarkBlue, ConsoleColor.Black),
                ConsoleThemeStyle.LevelInformation => (ConsoleColor.DarkCyan, ConsoleColor.Black),
                _ => (ConsoleColor.White, ConsoleColor.Black)
            };
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            return 0;
        }
    }
}
