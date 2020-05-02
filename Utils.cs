using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SponsorBoi
{
	public class Utils
	{
		public static string Minify(string input)
		{
			return JObject.Parse(input).ToString(Formatting.None);
		}

		public static string[] ParseIDs(string args)
		{
			if (string.IsNullOrEmpty(args))
			{
				return Array.Empty<string>();
			}
			return args.Trim().Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();
		}

		/// <summary>
		/// Gets the target of a command and checks if a user is allowed to use the command
		/// </summary>
		/// <param name="command">The command object.</param>
		/// <param name="permission">The name of the permission</param>
		/// <param name="argumentIndex">Index of the argument to check</param>
		/// <returns>The target if there is an allowed one, null if error or not allowed.</returns>
		public static async Task<DiscordUser> VerifyTargetUser(CommandContext command, string permission, int argumentIndex)
		{
			string[] args = ParseIDs(command?.RawArgumentString);
			string fullPermission = permission;
			ulong userID = command.Member.Id;

			// No ID/Mention provided
			if (args.Length <= argumentIndex)
			{
				fullPermission += ".self";
			}
			// Parse ID/Mention
			else if (ulong.TryParse(args[argumentIndex], out userID))
			{
				fullPermission += ".other";
			}
			// Argument provided but could not be parsed
			else
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync("", false, error);
				SponsorBoi.Debug(command.Member.Username + " tried to use the sync command but did not have permission.");
				return null;
			}

			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, fullPermission))
			{
				DiscordEmbed noPerm = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, noPerm);
				SponsorBoi.Debug(command.Member.Username + " tried to use the sync command but did not have permission.");
				return null;
			}

			try
			{
				return await command.Client.GetUserAsync(userID);
			}
			catch (NotFoundException)
			{
				SponsorBoi.Debug(command.Member.Username + " provided an invalid ID.");
				return null;
			}
		}
	}
}
