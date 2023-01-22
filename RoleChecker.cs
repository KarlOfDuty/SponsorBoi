using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SponsorBoi
{
	public static class RoleChecker
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
						Logger.Log("Started periodic check of sponsors...");
						await RunSponsorCheck();
						Logger.Log("Periodic sponsor check finished.");
					}
					catch (Exception e)
					{
						Logger.Error("Periodic sponsor check failed:\n" + e);
					}
					await Task.Delay(Config.autoPruneTime * 60 * 1000);
				}
			});
		}

		public static async Task RunSponsorCheck()
		{
			DiscordGuild guild;
			try
			{
				guild = await client.GetGuildAsync(Config.serverID);
			}
			catch (Exception)
			{
				Logger.Error("Error could not find Discord server with the configured id '" + Config.serverID + "'");
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
					Logger.Warn("Could not find user with id '" + linkedUser.discordID + "' on the server.");
					continue;
				}

				Logger.Log("Checking member: " + Utils.FullName(member));

				ulong sponsorTierRoleID = 0;
				Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == linkedUser.githubID);
				if (sponsor != null)
				{
					sponsorTierRoleID  = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
				}

				await SyncRoles(member, sponsorTierRoleID);
			}
		}

		public static async Task SyncRoles(DiscordMember member, ulong sponsorTierRoleID)
		{
			// Give them the appropriate role if they don't have it
			List<ulong> existingRoles = member.Roles.Select(x => x.Id).ToList();
			if (!existingRoles.Contains(sponsorTierRoleID) && sponsorTierRoleID != 0)
			{
				try
				{
					DiscordRole roleToGive = member.Guild.GetRole(sponsorTierRoleID);
					Logger.Log("Giving role '" + roleToGive.Name + "' to " + Utils.FullName(member));
					await member.GrantRoleAsync(roleToGive);
				}
				catch (Exception e)
				{
					Logger.Log("Error giving role <@" + sponsorTierRoleID + "> to user " + Utils.FullName(member) + ":\n" + e);
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
					DiscordRole roleToRemove = member.Guild.GetRole(removeRoleID);
					Logger.Log("Revoking role '" + roleToRemove.Name + "' from " + Utils.FullName(member));
					await member.RevokeRoleAsync(roleToRemove);
					await Task.Delay(1000);
				}
				catch (Exception e)
				{
					Logger.Log("Error removing role <@" + removeRoleID + "> from user " + Utils.FullName(member) + ":\n" + e);
				}
			}
		}
	}
}
