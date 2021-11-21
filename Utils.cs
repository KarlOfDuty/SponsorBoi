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

		public static async Task<bool> VerifyPermission(CommandContext command, string permission)
		{
			try
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, permission))
				{
					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					});
					return false;
				}

				return true;
			}
			catch (Exception)
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when checking permissions, please report this to the developer."
				});
				return false;
			}
		}

		public static async Task<bool> VerifySelfOtherPermission(CommandContext command, DiscordUser user, string permission)
		{
			try
			{
				string fullPermission = permission;

				if (user.Id == command.Member.Id)
				{
					fullPermission += ".self";
				}
				else
				{
					fullPermission += ".other";
				}

				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, fullPermission))
				{
					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					});
					return false;
				}

				return true;
			}
			catch (Exception)
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when checking permissions, please report this to the developer."
				});
				return false;
			}
		}

		public static string FullName(DiscordUser user)
		{
			return user.Username + "#" + user.Discriminator;
		}

		public static string FullName(DiscordMember user)
		{
			return user.Username + "#" + user.Discriminator;
		}
	}
}
