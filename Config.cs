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
		internal static uint updateTime = 5;

		internal static string botToken = "";
		internal static string prefix = "+";
		internal static string logLevel = "Info";
		internal static Dictionary<uint, ulong> tierRoles = new Dictionary<uint, ulong>();
		internal static string presenceType = "Playing";
		internal static string presenceText = "";

		internal static string hostName = "127.0.0.1";
		internal static int    port     = 3306;
		internal static string database = "sponsorboi";
		internal static string username = "";
		internal static string password = "";

		private static readonly Dictionary<string, ulong[]> permissions = new Dictionary<string, ulong[]>
		{
			{ "sync.self",     Array.Empty<ulong>() },
			{ "sync.other",    Array.Empty<ulong>() },
			{ "unsync.self",   Array.Empty<ulong>() },
			{ "unsync.other",  Array.Empty<ulong>() },
			{ "reload",        Array.Empty<ulong>() }
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

			githubToken = json.SelectToken("github.token").Value<string>() ?? "";
			updateTime = json.SelectToken("github.update-rate").Value<uint>();

			botToken = json.SelectToken("bot.token").Value<string>() ?? "";
			prefix = json.SelectToken("bot.prefix").Value<string>() ?? "";
			logLevel = json.SelectToken("bot.console-log-level").Value<string>() ?? "";

			foreach(JObject jo in json.SelectToken("bot.roles").Value<JArray>())
			{
				if (!uint.TryParse(jo.Properties().First().Name, out uint dollarAmount))
				{
					Logger.Warn(LogID.Config, "Could not parse dollar amount: '" + dollarAmount + "'");
					continue;
				}

				if (!ulong.TryParse(jo.Values().First().Value<string>(), out ulong roleID))
				{
					Logger.Warn(LogID.Config, "Could not parse roleID: '" + roleID + "'");
					continue;
				}

				tierRoles.Add(dollarAmount, roleID);
			}

			presenceType = json.SelectToken("bot.presence-type").Value<string>() ?? "";
			presenceText = json.SelectToken("bot.presence-text").Value<string>() ?? "";


			// Reads database info
			hostName = json.SelectToken("database.address").Value<string>() ?? "";
			port = json.SelectToken("database.port").Value<int>();
			database = json.SelectToken("database.name").Value<string>() ?? "";
			username = json.SelectToken("database.user").Value<string>() ?? "";
			password = json.SelectToken("database.password").Value<string>() ?? "";

			foreach ((string permissionName, ulong[] _) in permissions.ToList())
			{
				try
				{
					permissions[permissionName] = json.SelectToken("bot.permissions." + permissionName).Value<JArray>().Values<ulong>().ToArray();
				}
				catch (ArgumentNullException)
				{
					Console.WriteLine("Permission node '" + permissionName + "' was not found in the config, using default value: []");
				}
			}
		}

		public static bool HasPermission(DiscordMember member, string permission)
		{
			return member.Roles.Any(role => permissions[permission].Contains(role.Id)) || permissions[permission].Contains(member.Guild.Id);
		}

		public static bool TryGetTierRole(uint dollarAmount, out ulong roleID)
		{
			return tierRoles.TryGetValue(dollarAmount, out roleID);
		}
	}
}
