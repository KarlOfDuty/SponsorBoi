using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using SponsorBoi.Commands;

namespace SponsorBoi
{
	static class SponsorBoi
	{
		public const string APPLICATION_NAME = "SponsorBoi";

		// Sets up a dummy client to use for logging
		public static DiscordClient discordClient = new DiscordClient(new DiscordConfiguration { Token = "DUMMY_TOKEN", TokenType = TokenType.Bot, MinimumLogLevel = LogLevel.Debug });
		private static SlashCommandsExtension commands = null;

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
			Logger.Log("Starting " + APPLICATION_NAME + " version " + GetVersion() + "...");
			try
			{
				Initialize();

				// Block this task until the program is closed.
				await Task.Delay(-1);
			}
			catch (Exception e)
			{
				Logger.Fatal("Fatal error:\n" + e);
				Console.ReadLine();
			}
		}

		public static async void Initialize()
		{
			Logger.Log("Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();

			// Check if bot token is unset
			if (Config.botToken == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.botToken))
			{
				Logger.Fatal("You need to set your bot token in the config and start the bot again.");
				throw new ArgumentException("Invalid Discord bot token");
			}

			// Check if github token is unset
			if (Config.githubToken == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.githubToken))
			{
				Logger.Fatal("You need to set your Github personal access token in the config and start the bot again.");
				throw new ArgumentException("Invalid Github personal access token");
			}

			// Database connection and setup
			try
			{
				Logger.Log("Connecting to database...");
				Database.Initialize();
			}
			catch (Exception e)
			{
				Logger.Fatal("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
				throw;
			}

			Logger.Log("Setting up Github API client...");
			Github.Initialize();

			Logger.Log("Setting up Discord client...");

			// Checking log level
			if (!Enum.TryParse(Config.logLevel, true, out LogLevel logLevel))
			{
				Logger.Log("Log level " + Config.logLevel + " invalid, using 'Information' instead.");
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

			discordClient.UseInteractivity(new InteractivityConfiguration
			{
				AckPaginationButtons = true,
				PaginationBehaviour = PaginationBehaviour.Ignore,
				PaginationDeletion = PaginationDeletion.DeleteMessage,
				Timeout = TimeSpan.FromMinutes(15)
			});

			Logger.Log("Registering commands...");
			commands = discordClient.UseSlashCommands();
			commands.RegisterCommands<AdminLinkCommand>();
			commands.RegisterCommands<AdminUnlinkCommand>();
			commands.RegisterCommands<LinkCommand>();
			commands.RegisterCommands<RecheckCommand>();
			commands.RegisterCommands<UnlinkCommand>();
			commands.RegisterCommands<CreateSyncButtonCommand>();

			Logger.Log("Hooking events...");
			discordClient.Ready += EventHandler.OnReady;
			discordClient.GuildAvailable += EventHandler.OnGuildAvailable;
			discordClient.ClientErrored += EventHandler.OnClientError;
			discordClient.GuildMemberAdded += EventHandler.OnGuildMemberAdded;
			commands.SlashCommandErrored += EventHandler.OnCommandError;
			discordClient.ComponentInteractionCreated += EventHandler.OnComponentInteractionCreated;

			Logger.Log("Connecting to Discord...");
			await discordClient.ConnectAsync();

			RoleChecker.RunPeriodically();
		}
	}
}
