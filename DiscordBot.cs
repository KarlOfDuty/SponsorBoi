using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;

namespace DiscordBot
{
	class BotTemplate
	{
		internal static BotTemplate instance;
		private DiscordClient discordClient = null;
		private EventHandler eventHandler = null;

		static void Main(string[] args)
		{
			new BotTemplate().MainAsync().GetAwaiter().GetResult();
		}

		public static string GetVersion()
		{
			Version version = Assembly.GetEntryAssembly()?.GetName().Version;
			return version?.Major + "." + version?.Minor + "." + version?.Build + (version?.Revision == 0 ? "" : "-" + (char)(64 + version?.Revision ?? 0));
		}

		private async Task MainAsync()
		{
			instance = this;

			Console.WriteLine("Starting Discord Bot version " + GetVersion() + "...");
			try
			{
				this.Reload();

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

		public async void Reload()
		{
			if (this.discordClient != null)
			{
				await this.discordClient.DisconnectAsync();
				this.discordClient.Dispose();
				Console.WriteLine("Discord client disconnected.");
			}

			Console.WriteLine("Loading config \"" + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config.yml\"");
			Config.LoadConfig();

			// Check if token is unset
			if (Config.token == "<add-token-here>" || string.IsNullOrWhiteSpace(Config.token))
			{
				Console.WriteLine("You need to set your bot token in the config and start the bot again.");
				throw new ArgumentException("Invalid Discord bot token");
			}

			// Database connection and setup
			try
			{
				Console.WriteLine("Connecting to database...");
				Database.SetConnectionString(Config.hostName, Config.port, Config.database, Config.username, Config.password);
				Database.SetupTables();
			}
			catch (Exception e)
			{
				Console.WriteLine("Could not set up database tables, please confirm connection settings, status of the server and permissions of MySQL user. Error: " + e);
				throw;
			}

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
				Token = Config.token,
				TokenType = TokenType.Bot,

				AutoReconnect = true,
				LogLevel = logLevel,
				UseInternalLogHandler = true
			};

			this.discordClient = new DiscordClient(cfg);

			Console.WriteLine("Hooking events...");
			this.eventHandler = new EventHandler(this.discordClient);

			Console.WriteLine("Connecting to Discord...");
			await this.discordClient.ConnectAsync();
		}
	}
}
