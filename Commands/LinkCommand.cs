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

			if (Database.TryGetSponsor(githubAccount.id, out Database.SponsorEntry existingSponsor))
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
			ulong sponsorTierRoleID = 0;

			Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == githubAccount.id);
			if (sponsor != null)
			{
				sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
			}

			await RoleChecker.SyncRoles(targetMember, sponsorTierRoleID);
		}
	}
}
