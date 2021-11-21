using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SponsorBoi.Commands
{
	public class LinkCommand : BaseCommandModule
	{
		[Command("link")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			await ExecuteCommand(command, command.Member);
		}

		[Command("link")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, DiscordMember targetUser, string githubUsername)
		{
			await ExecuteCommand(command, targetUser, githubUsername);
		}

		private async Task ExecuteCommand(CommandContext command, DiscordMember targetMember, string githubUsername = null)
		{
			if (!await Utils.VerifySelfOtherPermission(command, targetMember, "link")) return;

			if (Database.TryGetSponsor(targetMember.Id, out Database.SponsorEntry _))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Discord account <@" + targetMember.Id + "> is already linked to a Github account."
				};
				await command.RespondAsync(error);
				return;
			}

			Github.Account githubAccount = null;
			if (githubUsername != null) // The username has been provided as a command argument
			{
				githubAccount = await Github.GetUserByUsername(githubUsername);
				if (githubAccount == null)
				{
					DiscordEmbed notfound = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not find Github user '" + githubUsername + "'."
					};
					await command.RespondAsync(notfound);
					return;
				}
			}
			else // Perform standard two-factor authentication as username has not been provided
			{
				List<Github.Issue> issues = await Github.GetIssues();
				foreach (Github.Issue issue in issues)
				{
					if (issue.description.Contains(command.Member.Id.ToString()))
					{
						githubAccount = issue.author;
						break;
					}
				}

				if (githubAccount == null)
				{
					DiscordEmbed notfound = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not find a recent Github issue containing your Discord ID.\n"
									+ "You must create an issue containing your Discord ID here:\n" + Config.issueURL
					};
					await command.RespondAsync(notfound);
					return;
				}
			}

			if (!Database.TryGetSponsor(githubAccount.id, out Database.SponsorEntry existingSponsor))
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Github account '" + githubAccount.name + "' is already linked to <@" + existingSponsor.discordID + ">"
				});
				return;
			}

			if (!Database.TryAddSponsor(new Database.SponsorEntry { discordID = targetMember.Id, githubID = githubAccount.id }))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when writing sponsor to database."
				};
				await command.RespondAsync(error);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "The Github account '" + githubAccount.name + "' is now linked to <@" + targetMember.Id + ">"
			};
			await command.RespondAsync(message);

			List<Github.Sponsor> sponsors = await Github.GetSponsors();
			int dollarAmount = sponsors.FirstOrDefault(x => x.sponsor.id == githubAccount.id).dollarAmount;
			ulong sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(dollarAmount);

			List<ulong> forbiddenRoles = Config.tierRoles.Values.ToList();
			forbiddenRoles.RemoveAll(x => x == sponsorTierRoleID);

			// Give them the appropriate role if they don't have it
			List<ulong> existingRoles = targetMember.Roles.Select(x => x.Id).ToList();
			if (!existingRoles.Contains(sponsorTierRoleID) && sponsorTierRoleID != 0)
			{
				try
				{
					DiscordRole roleToGive = command.Guild.GetRole(sponsorTierRoleID);
					Logger.Log(LogID.Discord, "Giving role '" + roleToGive.Name + "' to " + Utils.FullName(targetMember));
					await targetMember.GrantRoleAsync(roleToGive);

					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Role '" + roleToGive.Name + "' granted to <@" + targetMember.Id + ">!"
					});
				}
				catch (Exception e)
				{
					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error giving role <@" + sponsorTierRoleID + "> to user <@" + targetMember.Id + ">!"
					});
					Logger.Log(LogID.Discord, "Error giving role <@" + sponsorTierRoleID + "> to user " + Utils.FullName(targetMember) + ":\n" + e);
				}
			}

			// Remove all inappropriate roles they have
			List<ulong> rolesToRemove = forbiddenRoles.Where(x => existingRoles.Contains(x)).ToList();
			foreach (ulong removeRoleID in rolesToRemove)
			{
				try
				{
					DiscordRole roleToRemove = command.Guild.GetRole(removeRoleID);
					Logger.Log(LogID.Discord, "Revoking role '" + roleToRemove.Name + "' from " + Utils.FullName(targetMember));
					await targetMember.RevokeRoleAsync(roleToRemove);

					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Role '" + roleToRemove.Name + "' removed from <@" + targetMember.Id + ">!"
					});
					await Task.Delay(1000);
				}
				catch (Exception e)
				{
					await command.RespondAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error removing role <@" + removeRoleID + "> from user <@" + targetMember.Id + ">!"
					});
					Logger.Log(LogID.Discord, "Error removing role <@" + removeRoleID + "> from user " + Utils.FullName(targetMember) + ":\n" + e);
				}
			}
		}
	}
}
