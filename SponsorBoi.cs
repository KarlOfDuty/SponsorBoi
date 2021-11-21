using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using SponsorBoi.Commands;

namespace SponsorBoi
{
	static class SponsorBoi
	{
		public const string APPLICATION_NAME = "SponsorBoi";

		// Sets up a dummy client to use for logging
		public static DiscordClient discordClient = new DiscordClient(new DiscordConfiguration { Token = "DUMMY_TOKEN", TokenType = TokenType.Bot, MinimumLogLevel = LogLevel.Debug });

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
			Logger.Log(LogID.General, "Starting " + APPLICATION_NAME + " version " + GetVersion() + "...");
			try
			{
				Initialize();

				// Block this task until the program is closed.
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Logger.Fatal(LogID.General, "Fatal error:\n" + e);
				Console.ReadLine();
			}
		}

		public static async void Initialize()
		{
			Logger.Log(LogID.General, "Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();

			// Check if bot token is unset
			if (Config.botToken == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.botToken))
			{
				Logger.Fatal(LogID.Config, "You need to set your bot token in the config and start the bot again.");
				throw new ArgumentException("Invalid Discord bot token");
			}

			// Check if github token is unset
			if (Config.githubToken == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.githubToken))
			{
				Logger.Fatal(LogID.Config, "You need to set your Github personal access token in the config and start the bot again.");
				throw new ArgumentException("Invalid Github personal access token");
			}

			// Database connection and setup
			try
			{
				Logger.Log(LogID.General, "Connecting to database...");
				Database.Initialize();
			}
			catch (Exception e)
			{
				Logger.Fatal(LogID.General, "Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
				throw;
			}

			Logger.Log(LogID.General, "Setting up Github API client...");
			Github.Initialize();

			Logger.Log(LogID.General, "Setting up Discord client...");

			// Checking log level
			if (!Enum.TryParse(Config.logLevel, true, out LogLevel logLevel))
			{
				Logger.Log(LogID.General, "Log level " + Config.logLevel + " invalid, using 'Information' instead.");
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

			Logger.Log(LogID.General, "Hooking commands...");
			CommandsNextExtension commands = discordClient.UseCommandsNext(new CommandsNextConfiguration
			{
				StringPrefixes = new[] { Config.prefix }
			});
			commands.RegisterCommands<LinkCommand>();
			commands.RegisterCommands<UnlinkCommand>();
			commands.RegisterCommands<RecheckCommand>();

			Logger.Log(LogID.General, "Hooking events...");
			discordClient.Ready += EventHandler.OnReady;
			discordClient.GuildAvailable += EventHandler.OnGuildAvailable;
			discordClient.ClientErrored += EventHandler.OnClientError;
			discordClient.GuildMemberAdded += EventHandler.OnGuildMemberAdded;
			commands.CommandErrored += EventHandler.OnCommandError;

			Logger.Log(LogID.General, "Connecting to Discord...");
			await discordClient.ConnectAsync();

			PeriodicRechecker.RunPeriodically();
		}
	}
}
