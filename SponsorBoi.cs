using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;

namespace SponsorBoi
{ 
	static class SponsorBoi
	{
		internal const string APPLICATION_NAME = "SponsorBoi";

		private static DiscordClient discordClient = null;

		static void Main(string[] args)
		{
			MainAsync().GetAwaiter().GetResult();
		}

		public static string GetVersion()
		{
			Version version = Assembly.GetEntryAssembly()?.GetName().Version;
			return version?.Major + "." + version?.Minor + "." + version?.Build + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0));
		}

		private static async Task MainAsync()
		{
			Console.WriteLine("Starting " + APPLICATION_NAME + " version " + GetVersion() + "...");
			try
			{
				Reload();

				await Github.GetSponsors();

				// Block this task until the program is closed.
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Console.WriteLine("Fatal error:");
				Console.WriteLine(e);
				Console.ReadLine();
			}
		}

		internal static void Info(string message)
		{
			discordClient.DebugLogger.LogMessage(LogLevel.Info, APPLICATION_NAME, message, DateTime.UtcNow);
		}

		internal static void Warning(string message)
		{
			discordClient.DebugLogger.LogMessage(LogLevel.Warning, APPLICATION_NAME, message, DateTime.UtcNow);
		}

		internal static void Debug(string message)
		{
			discordClient.DebugLogger.LogMessage(LogLevel.Debug, APPLICATION_NAME, message, DateTime.UtcNow);
		}

		internal static async void Reload()
		{
			if (discordClient != null)
			{
				await discordClient.DisconnectAsync();
				discordClient.Dispose();
				Console.WriteLine("Discord client disconnected.");
			}

			Console.WriteLine("Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();

			// Check if bot token is unset
			if (Config.botToken == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.botToken))
			{
				Console.WriteLine("You need to set your bot token in the config and start the bot again.");
				throw new ArgumentException("Invalid Discord bot token");
			}

			// Check if github token is unset
			if (Config.githubToken == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.githubToken))
			{
				Console.WriteLine("You need to set your Github personal access token in the config and start the bot again.");
				throw new ArgumentException("Invalid Github personal access token");
			}

			// Database connection and setup
			try
			{
				Console.WriteLine("Connecting to database...");
				Database.Initialize();
			}
			catch (Exception e)
			{
				Console.WriteLine("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
				throw;
			}

			Console.WriteLine("Setting up Github API client...");
			Github.Initialize();

			Console.WriteLine("Setting up Discord client...");

			// Checking log level
			if (!Enum.TryParse(Config.logLevel, true, out LogLevel logLevel))
			{
				Console.WriteLine("Log level " + Config.logLevel + " invalid, using 'Info' instead.");
				logLevel = LogLevel.Info;
			}

			// Setting up client configuration
			DiscordConfiguration cfg = new DiscordConfiguration
			{
				Token = Config.botToken,
				TokenType = TokenType.Bot,

				AutoReconnect = true,
				LogLevel = logLevel,
				UseInternalLogHandler = true
			};

			discordClient = new DiscordClient(cfg);

			Console.WriteLine("Hooking events...");
			EventHandler.Initialize(discordClient);

			Console.WriteLine("Connecting to Discord...");
			await discordClient.ConnectAsync();
		}
	}
}
