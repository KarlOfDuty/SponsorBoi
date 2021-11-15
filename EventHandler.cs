using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace SponsorBoi
{
	internal static class EventHandler
	{
		private static DiscordClient discordClient;
		internal static void Initialize(DiscordClient client)
		{
			discordClient = client;
			
			discordClient.Ready += OnReady;
			discordClient.GuildAvailable += OnGuildAvailable;
			discordClient.ClientErrored += OnClientError;
			discordClient.GuildMemberAdded += OnGuildMemberAdded;
		}

		private static Task OnReady(DiscordClient client, ReadyEventArgs e)
		{
			Logger.Log(LogID.Discord, "Client is ready to process events.");

			// Checking activity type
			if (!Enum.TryParse(Config.presenceType, true, out ActivityType activityType))
			{
				Console.WriteLine("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
				activityType = ActivityType.Playing;
			}

			discordClient.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), UserStatus.Online);
			return Task.CompletedTask;
		}

		private static Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs e)
		{
			Logger.Log(LogID.Discord, "Guild available: " + e.Guild.Name);

			IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

			foreach ((ulong roleID, DiscordRole role) in roles)
			{
				Logger.Log(LogID.Discord, role.Name.PadRight(40, '.') + roleID);
			}
			return Task.CompletedTask;
		}

		private static Task OnClientError(DiscordClient client, ClientErrorEventArgs e)
		{
			Logger.Error(LogID.Discord, $"Exception occured: {e.Exception.GetType()}: {e.Exception}");
			return Task.CompletedTask;
		}

		private static async Task OnGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
		{
			// Check if user has registered their Github account
			if (!Database.TryGetSponsor(e.Member.Id, out Database.SponsorEntry sponsorEntry)) return;

			// Check if user is active sponsor
			List <Github.Sponsor> sponsors = await Github.GetSponsors();
			Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.username != sponsorEntry.githubUser);
			if (sponsor == null) return;

			// Check if tier is registered in the config
			if (!Config.TryGetTierRole(sponsor.dollarAmount, out ulong roleID)) return;

			// Assign role
			DiscordRole role = e.Guild.GetRole(roleID);
			Logger.Log(LogID.Discord, e.Member.DisplayName + " (" + e.Member.Id + ") were given back the role '" + role.Name + "' on rejoin. ");
			await e.Member.GrantRoleAsync(role);
		}
	}
}
