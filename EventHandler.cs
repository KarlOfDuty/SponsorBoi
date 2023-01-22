using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;

namespace SponsorBoi
{
	public static class EventHandler
	{
		public static Task OnReady(DiscordClient client, ReadyEventArgs e)
		{
			Logger.Log("Client is ready to process events.");

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
			Logger.Log("Guild available: " + e.Guild.Name);

			IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

			foreach ((ulong roleID, DiscordRole role) in roles)
			{
				Logger.Log(role.Name.PadRight(40, '.') + roleID);
			}
			return Task.CompletedTask;
		}

		public static Task OnClientError(DiscordClient client, ClientErrorEventArgs e)
		{
			Logger.Error("Exception occured:\n" + e.Exception);
			return Task.CompletedTask;
		}

		public static async Task OnGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
		{
			if (e.Guild.Id != Config.serverID) return;

			// Check if user has registered their Github account
			if (!Database.TryGetSponsor(e.Member.Id, out Database.SponsorEntry sponsorEntry)) return;

			// Check if user is active sponsor
			List <Github.Sponsor> sponsors = await Github.GetSponsors();
			Github.Sponsor sponsor = sponsors.FirstOrDefault(s => s.sponsor.id == sponsorEntry.githubID);
			if (sponsor == null) return;

			// Check if tier is registered in the config
			if (!Config.TryGetTierRole(sponsor.dollarAmount, out ulong roleID)) return;

			// Assign role
			DiscordRole role = e.Guild.GetRole(roleID);
			Logger.Log(Utils.FullName(e.Member) + " (" + e.Member.Id + ") were given back the role '" + role.Name + "' on rejoin. ");
			await e.Member.GrantRoleAsync(role);
		}

		internal static async Task OnCommandError(SlashCommandsExtension commandSystem, SlashCommandErrorEventArgs e)
		{
			switch (e.Exception)
			{
				case SlashExecutionChecksFailedException checksFailedException:
				{
					foreach (SlashCheckBaseAttribute attr in checksFailedException.FailedChecks)
					{
						await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = ParseFailedCheck(attr)
						});
					}
					return;
				}

				case BadRequestException ex:
					Logger.Error("Command exception occured:\n" + e.Exception);
					Logger.Error("JSON Message: " + ex.JsonMessage);
					return;

				default:
				{
					Logger.Error("Exception occured: " + e.Exception.GetType() + ": " + e.Exception);
					await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Internal error occured, please report this to the developer."
					});
					return;
				}
			}
		}

		private static string ParseFailedCheck(SlashCheckBaseAttribute attr)
		{
			return attr switch
			{
				SlashRequireDirectMessageAttribute => "This command can only be used in direct messages!",
				SlashRequireOwnerAttribute => "Only the server owner can use that command!",
				SlashRequirePermissionsAttribute => "You don't have permission to do that!",
				SlashRequireBotPermissionsAttribute => "The bot doesn't have the required permissions to do that!",
				SlashRequireUserPermissionsAttribute => "You don't have permission to do that!",
				SlashRequireGuildAttribute => "This command has to be used in a Discord server!",
				_ => "Unknown Discord API error occured, please try again later."
			};
		}
	}
}
