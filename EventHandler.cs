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

		private static Task OnReady(ReadyEventArgs e)
		{
			discordClient.DebugLogger.LogMessage(LogLevel.Info, SponsorBoi.APPLICATION_NAME, "Client is ready to process events.", DateTime.UtcNow);
			discordClient.UpdateStatusAsync(new DiscordGame("Github Sponsors"), UserStatus.Online);
			return Task.CompletedTask;
		}

		private static Task OnGuildAvailable(GuildCreateEventArgs e)
		{
			discordClient.DebugLogger.LogMessage(LogLevel.Info, SponsorBoi.APPLICATION_NAME, $"Guild available: {e.Guild.Name}", DateTime.UtcNow);

			IReadOnlyList<DiscordRole> roles = e.Guild.Roles;

			foreach (DiscordRole role in roles)
			{
				discordClient.DebugLogger.LogMessage(LogLevel.Info, SponsorBoi.APPLICATION_NAME, role.Name.PadRight(40, '.') + role.Id, DateTime.UtcNow);
			}
			return Task.CompletedTask;
		}

		private static Task OnClientError(ClientErrorEventArgs e)
		{
			discordClient.DebugLogger.LogMessage(LogLevel.Error, SponsorBoi.APPLICATION_NAME, $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.UtcNow);
			return Task.CompletedTask;
		}

		private static async Task OnGuildMemberAdded(GuildMemberAddEventArgs e)
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
			discordClient.DebugLogger.LogMessage(LogLevel.Info, SponsorBoi.APPLICATION_NAME, e.Member.DisplayName + " (" + e.Member.Id + ") were given back the role '" + role.Name + "' on rejoin. ", DateTime.UtcNow);
			await e.Member.GrantRoleAsync(role);
		}
	}
}
