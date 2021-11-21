using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using SponsorBoi.Properties;
using YamlDotNet.Serialization;

namespace SponsorBoi
{
	internal static class Config
	{
		internal static string githubToken = "";
		internal static string repositoryName = "";
		internal static string issueURL = "<url missing in config>";
		internal static int autoPruneTime = 120;

		internal static string botToken = "";
		internal static string prefix = "+";
		internal static string logLevel = "Info";
		internal static Dictionary<int, ulong> tierRoles = new Dictionary<int, ulong>();
		internal static string presenceType = "Playing";
		internal static string presenceText = "";
		internal static ulong serverID = 0;

		internal static string hostName = "127.0.0.1";
		internal static int    port     = 3306;
		internal static string database = "sponsorboi";
		internal static string username = "";
		internal static string password = "";

		private static readonly Dictionary<string, ulong[]> permissions = new Dictionary<string, ulong[]>
		{
			{ "link.self",     Array.Empty<ulong>() },
			{ "link.other",    Array.Empty<ulong>() },
			{ "unlink.self",   Array.Empty<ulong>() },
			{ "unlink.other",  Array.Empty<ulong>() },
			{ "recheck",       Array.Empty<ulong>() }
		};

		public static void LoadConfig()
		{
			// Writes default config to file if it does not already exist
			if (!File.Exists("./config.yml"))
			{
				File.WriteAllText("./config.yml", Encoding.UTF8.GetString(Resources.default_config));
			}

			// Reads config contents into FileStream
			FileStream stream = File.OpenRead("./config.yml");

			// Converts the FileStream into a YAML object
			IDeserializer deserializer = new DeserializerBuilder().Build();
			object yamlObject = deserializer.Deserialize(new StreamReader(stream));

			// Converts the YAML object into a JSON object as the YAML ones do not support traversal or selection of nodes by name 
			ISerializer serializer = new SerializerBuilder().JsonCompatible().Build();
			JObject json = JObject.Parse(serializer.Serialize(yamlObject));

			githubToken = json.SelectToken("github.token")?.Value<string>() ?? githubToken;
			autoPruneTime = json.SelectToken("github.auto-prune-time")?.Value<int>() ?? autoPruneTime;
			repositoryName = json.SelectToken("github.sync.repository-name")?.Value<string>() ?? repositoryName;
			issueURL = json.SelectToken("github.sync.issue-url")?.Value<string>() ?? issueURL;
			if (string.IsNullOrWhiteSpace(issueURL))
			{
				Logger.Warn(LogID.Config, "Issue URL was not set in your config, some messages will not show correctly!");
			}

			botToken = json.SelectToken("bot.token")?.Value<string>() ?? botToken;
			prefix = json.SelectToken("bot.prefix")?.Value<string>() ?? prefix;
			logLevel = json.SelectToken("bot.console-log-level")?.Value<string>() ?? logLevel;

			foreach(JObject jo in json.SelectToken("bot.roles")?.Value<JArray>())
			{
				if (!int.TryParse(jo.Properties()?.First()?.Name, out int dollarAmount))
				{
					Logger.Warn(LogID.Config, "Could not parse dollar amount: '" + dollarAmount + "'");
					continue;
				}

				if (!ulong.TryParse(jo.Values()?.First()?.Value<string>(), out ulong roleID))
				{
					Logger.Warn(LogID.Config, "Could not parse roleID: '" + roleID + "'");
					continue;
				}

				tierRoles.Add(dollarAmount, roleID);
			}

			presenceType = json.SelectToken("bot.presence-type")?.Value<string>() ?? presenceType;
			presenceText = json.SelectToken("bot.presence-text")?.Value<string>() ?? presenceText;
			serverID = json.SelectToken("bot.server-id")?.Value<ulong>() ?? serverID;

			hostName = json.SelectToken("database.address")?.Value<string>() ?? hostName;
			port = json.SelectToken("database.port")?.Value<int>() ?? port;
			database = json.SelectToken("database.name")?.Value<string>() ?? database;
			username = json.SelectToken("database.user")?.Value<string>() ?? username;
			password = json.SelectToken("database.password")?.Value<string>() ?? password;

			foreach ((string permissionName, ulong[] _) in permissions.ToList())
			{
				try
				{
					permissions[permissionName] = json.SelectToken("bot.permissions." + permissionName).Value<JArray>().Values<ulong>().ToArray();
				}
				catch (ArgumentNullException)
				{
					Logger.Warn(LogID.Config, "Permission node '" + permissionName + "' was not found in the config, using default value: []");
				}
			}
		}

		public static bool HasPermission(DiscordMember member, string permission)
		{
			return member.Roles.Any(role => permissions[permission].Contains(role.Id)) || permissions[permission].Contains(member.Guild.Id);
		}

		public static bool TryGetTierRole(int dollarAmount, out ulong roleID)
		{
			return tierRoles.TryGetValue(dollarAmount, out roleID);
		}
	}
}
