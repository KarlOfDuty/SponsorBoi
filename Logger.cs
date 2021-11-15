using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace SponsorBoi
{
	public enum LogID
	{
		General,
		Config,
		Github,
		Command,
		Discord
	};

	public static class Logger
	{
		private static Dictionary<LogID, EventId> eventIDs = new Dictionary<LogID, EventId>
		{
			{ LogID.General, new EventId(500, "General") },
			{ LogID.Config,  new EventId(501, "Config")  },
			{ LogID.Github,  new EventId(502, "Github")  },
			{ LogID.Command, new EventId(503, "Command") },
			{ LogID.Discord, new EventId(504, "Discord") },
		};

		public static void Debug(LogID logID, string Message)
		{
			try
			{
				SponsorBoi.discordClient.Logger.Log(LogLevel.Debug, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[DEBUG] " + Message);
			}
		}

		public static void Log(LogID logID, string Message)
		{
			try
			{
				SponsorBoi.discordClient.Logger.Log(LogLevel.Information, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[INFO] " + Message);
			}
		}

		public static void Warn(LogID logID, string Message)
		{
			try
			{
				SponsorBoi.discordClient.Logger.Log(LogLevel.Warning, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[WARNING] " + Message);
			}
		}

		public static void Error(LogID logID, string Message)
		{
			try
			{
				SponsorBoi.discordClient.Logger.Log(LogLevel.Error, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[ERROR] " + Message);
			}
		}

		public static void Fatal(LogID logID, string Message)
		{
			try
			{
				SponsorBoi.discordClient.Logger.Log(LogLevel.Critical, eventIDs[logID], Message);
			}
			catch (NullReferenceException)
			{
				Console.WriteLine("[CRITICAL] " + Message);
			}
		}
	}
}
