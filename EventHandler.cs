using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace SponsorBoi
{
	public static class EventHandler
	{
		public static Task OnReady(DiscordClient client, ReadyEventArgs e)
		{
			Logger.Log(LogID.Discord, "Client is ready to process events.");

			// Checking activity type
			if (!Enum.TryParse(Config.presenceType, true, out ActivityType activityType))
			{
				Console.WriteLine("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
				activityType = ActivityType.Playing;
			}

			client.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), UserStatus.Online);
			return Task.CompletedTask;
		}

		public static Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs e)
		{
			Logger.Log(LogID.Discord, "Guild available: " + e.Guild.Name);

			IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

			foreach ((ulong roleID, DiscordRole role) in roles)
			{
				Logger.Log(LogID.Discord, role.Name.PadRight(40, '.') + roleID);
			}
			return Task.CompletedTask;
		}

		public static Task OnClientError(DiscordClient client, ClientErrorEventArgs e)
		{
			Logger.Error(LogID.Discord, "Exception occured:\n" + e.Exception);
			return Task.CompletedTask;
		}

		public static async Task OnGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
		{
			if (e.Guild.Id != Config.serverID) return;

			// Check if user has registered their Github account
			if (!Database.TryGetSponsor(e.Member.Id, out Database.SponsorEntry sponsorEntry)) return;

			// Check if user is active sponsor
			List <Github.Sponsor> sponsors = await Github.GetCachedSponsors();
			Github.Sponsor sponsor = sponsors.FirstOrDefault(s => s.sponsor.id == sponsorEntry.githubID);
			if (sponsor == null) return;

			// Check if tier is registered in the config
			if (!Config.TryGetTierRole(sponsor.dollarAmount, out ulong roleID)) return;

			// Assign role
			DiscordRole role = e.Guild.GetRole(roleID);
			Logger.Log(LogID.Discord, Utils.FullName(e.Member) + " (" + e.Member.Id + ") were given back the role '" + role.Name + "' on rejoin. ");
			await e.Member.GrantRoleAsync(role);
		}

		public static Task OnCommandError(CommandsNextExtension commandSystem, CommandErrorEventArgs e)
		{
			switch (e.Exception)
			{
				case CommandNotFoundException _:
					return Task.CompletedTask;
				case ChecksFailedException _:
				{
					foreach (CheckBaseAttribute attr in ((ChecksFailedException)e.Exception).FailedChecks)
					{
						DiscordEmbed error = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = ParseFailedCheck(attr)
						};
						e.Context?.Channel?.SendMessageAsync(error);
					}
					return Task.CompletedTask;
				}

				default:
				{
					Logger.Error(LogID.Discord, "Exception occured: \n" + e.Exception);
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Internal error occured, please report this to the developer."
					};
					e.Context?.Channel?.SendMessageAsync(error);
					return Task.CompletedTask;
				}
			}
		}

		private static string ParseFailedCheck(CheckBaseAttribute attr)
		{
			switch (attr)
			{
				case CooldownAttribute _:
					return "You cannot use do that so often!";
				case RequireOwnerAttribute _:
					return "Only the server owner can use that command!";
				case RequirePermissionsAttribute _:
					return "You don't have permission to do that!";
				case RequireRolesAttribute _:
					return "You do not have a required role!";
				case RequireUserPermissionsAttribute _:
					return "You don't have permission to do that!";
				case RequireNsfwAttribute _:
					return "This command can only be used in an NSFW channel!";
				default:
					return "Unknown Discord API error occured, please try again later.";
			}
		}
	}
}
