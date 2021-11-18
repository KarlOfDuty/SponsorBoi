using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace SponsorBoi
{ 
	static class SponsorBoi
	{
		public const string APPLICATION_NAME = "SponsorBoi";

		public static DiscordClient discordClient = null;

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

				List<Github.Sponsor> sponsors = await Github.GetSponsors();
				List<Github.Issue> issues = await Github.GetIssues();

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
				Console.WriteLine("Log level " + Config.logLevel + " invalid, using 'Information' instead.");
				logLevel = LogLevel.Information;
			}

			// Setting up client configuration
			DiscordConfiguration cfg = new DiscordConfiguration
			{
				Token = Config.botToken,
				TokenType = TokenType.Bot,
				MinimumLogLevel = logLevel,
				AutoReconnect = true,
				Intents = DiscordIntents.All
			};

			discordClient = new DiscordClient(cfg);

			Console.WriteLine("Hooking events...");
			EventHandler.Initialize(discordClient);

			Console.WriteLine("Connecting to Discord...");
			await discordClient.ConnectAsync();
		}
	}
}
