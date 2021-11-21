using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SponsorBoi.Commands
{
	public class RecheckCommand : BaseCommandModule
	{
		[Command("recheck")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			if (!await Utils.VerifyPermission(command, "recheck")) return;

			await command.RespondAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Starting recheck on all sponsors..."
			});

			await PeriodicRechecker.RunManually();

			await command.RespondAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Recheck complete."
			});
		}

		[Command("recheck")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, DiscordMember member)
		{
			if (!await Utils.VerifyPermission(command, "recheck")) return;

			if (!Database.TryGetSponsor(member.Id, out Database.SponsorEntry sponsorEntry))
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "That user isn't linked to a Github account."
				});
				return;
			}

			await command.RespondAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Starting recheck on " + member.Mention + "..."
			});

			List<Github.Sponsor> sponsors = await Github.GetSponsors();
			List<Database.SponsorEntry> linkedUsers = Database.GetAllSponsors();

			foreach (Database.SponsorEntry linkedUser in linkedUsers)
			{
				ulong sponsorTierRoleID = 0;
				Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == linkedUser.githubID);
				if (sponsor != null)
				{
					sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
				}

				// Give them the appropriate role if they don't have it
				List<ulong> existingRoles = member.Roles.Select(x => x.Id).ToList();
				if (!existingRoles.Contains(sponsorTierRoleID) && sponsorTierRoleID != 0)
				{
					try
					{
						DiscordRole roleToGive = command.Guild.GetRole(sponsorTierRoleID);
						Logger.Log(LogID.Discord, "Giving role '" + roleToGive.Name + "' to " + Utils.FullName(member));
						await member.GrantRoleAsync(roleToGive);

						await command.RespondAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = "Role '" + roleToGive.Name + "' granted to <@" + member.Id + ">!"
						});
					}
					catch (Exception e)
					{
						await command.RespondAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error giving role <@" + sponsorTierRoleID + "> to user <@" + member.Id + ">!"
						});
						Logger.Log(LogID.Discord, "Error giving role <@" + sponsorTierRoleID + "> to user " + Utils.FullName(member) + ":\n" + e);
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
						DiscordRole roleToRemove = command.Guild.GetRole(removeRoleID);
						Logger.Log(LogID.Discord, "Revoking role '" + roleToRemove.Name + "' from " + Utils.FullName(member));
						await member.RevokeRoleAsync(roleToRemove);

						await command.RespondAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Role '" + roleToRemove.Name + "' removed from <@" + member.Id + ">!"
						});
						await Task.Delay(1000);
					}
					catch (Exception e)
					{
						await command.RespondAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error removing role <@" + removeRoleID + "> from user <@" + member.Id + ">!"
						});
						Logger.Log(LogID.Discord, "Error removing role <@" + removeRoleID + "> from user " + Utils.FullName(member) + ":\n" + e);
					}
				}
			}

			await command.RespondAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Recheck complete."
			});
		}
	}
}
