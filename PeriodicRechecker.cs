using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SponsorBoi
{
	public static class PeriodicRechecker
	{
		public static DiscordClient client = null;
		public static void RunPeriodically()
		{
			client = SponsorBoi.discordClient;

			Task.Run(async () => {
				await Task.Delay(10 * 1000);
				while (true)
				{
					try
					{
						Logger.Log(LogID.Discord, "Started periodic check of sponsors...");
						await CheckSponsors();
						Logger.Log(LogID.Discord, "Periodic sponsor check finished.");
					}
					catch (Exception e)
					{
						Logger.Error(LogID.Discord, "Periodic sponsor check failed:\n" + e);
					}
					await Task.Delay(Config.autoPruneTime * 60 * 1000);
				}
			});
		}

		public static async Task RunManually()
		{
			Logger.Log(LogID.Discord, "Started manual check of sponsors...");
			await CheckSponsors();
			Logger.Log(LogID.Discord, "Manual sponsor check finished.");
		}

		private static async Task CheckSponsors()
		{
			DiscordGuild guild;
			try
			{
				guild = await client.GetGuildAsync(Config.serverID);
			}
			catch (Exception)
			{
				Logger.Error(LogID.Discord, "Error could not find Discord server with the configured id '" + Config.serverID + "'");
				return;
			}

			List<Github.Sponsor> sponsors = await Github.GetSponsors();
			List<Database.SponsorEntry> linkedUsers = Database.GetAllSponsors();

			foreach (Database.SponsorEntry linkedUser in linkedUsers)
			{
				DiscordMember member;
				try
				{
					member = await guild.GetMemberAsync(linkedUser.discordID);
					if (member == null) throw new Exception();
				}
				catch (Exception)
				{
					Logger.Warn(LogID.Discord, "Could not find user with id '" + linkedUser.discordID + "' on the server.");
					continue;
				}

				Logger.Log(LogID.Discord, "Checking member: " + Utils.FullName(member));

				ulong sponsorTierRoleID = 0;
				Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == linkedUser.githubID);
				if (sponsor != null)
				{
					sponsorTierRoleID  = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
				}

				// Give them the appropriate role if they don't have it
				List<ulong> existingRoles = member.Roles.Select(x => x.Id).ToList();
				if (!existingRoles.Contains(sponsorTierRoleID) && sponsorTierRoleID != 0)
				{
					try
					{
						DiscordRole roleToGive = guild.GetRole(sponsorTierRoleID);
						Logger.Log(LogID.Discord, "Giving role '" + roleToGive.Name + "' to " + Utils.FullName(member));
						await member.GrantRoleAsync(roleToGive);
						await Task.Delay(1000);
					}
					catch (Exception e)
					{
						Logger.Error(LogID.Discord, "Error giving role " + sponsorTierRoleID + " to user " + Utils.FullName(member) + ":\n" + e);
					}
				}

				// Remove all inappropriate roles they have
				List<ulong> forbiddenRoles = Config.tierRoles.Values.ToList();
				forbiddenRoles.RemoveAll(x => x == sponsorTierRoleID);
				List<ulong> rolesToRemove = forbiddenRoles.Where(x => existingRoles.Contains(x)).ToList();
				foreach (ulong removeRoleID in rolesToRemove)
				{
					try
					{
						DiscordRole roleToRemove = guild.GetRole(removeRoleID);
						Logger.Log(LogID.Discord, "Revoking role '" + roleToRemove.Name + "' from " + Utils.FullName(member));
						await member.RevokeRoleAsync(roleToRemove);
						await Task.Delay(1000);
					}
					catch (Exception e)
					{
						Logger.Error(LogID.Discord, "Error removing role " + removeRoleID + " from user " + Utils.FullName(member) + ":\n" + e);
					}
				}
				
			}
		}
	}
}
