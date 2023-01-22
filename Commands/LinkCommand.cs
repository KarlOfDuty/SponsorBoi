﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands
{
	public class LinkCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("link", "Link your Github account for Sponsor syncing")]
		private async Task ExecuteCommand(InteractionContext command)
		{
			if (Database.TryGetSponsor(command.Member.Id, out Database.SponsorEntry _))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Discord account " + command.Member.Mention + " is already linked to a Github account."
				}, true);
				return;
			}

			Github.Account githubAccount = null;
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
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Cyan,
					Description = "Click [here](" + Utils.GetIssueURL("Discord ID: " + command.Member.Id) + ")"
								  + " and click submit in order to verify your account. Run this command again when done.",
				}, true);
				return;
			}

			if (Database.TryGetSponsor(githubAccount.id, out Database.SponsorEntry existingSponsor))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Github account '" + githubAccount.name + "' is already linked to <@" + existingSponsor.discordID + ">"
				}, true);
				return;
			}

			if (!Database.TryAddSponsor(new Database.SponsorEntry { discordID = command.Member.Id, githubID = githubAccount.id }))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when writing sponsor to database."
				}, true);
				return;
			}

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "The Github account '" + githubAccount.name + "' is now linked to " + command.Member.Mention + "\n\n"
							  + "You may now close or delete the Github issue."
			}, true);

			List<Github.Sponsor> sponsors = await Github.GetSponsors();
			ulong sponsorTierRoleID = 0;

			Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == githubAccount.id);
			if (sponsor != null)
			{
				sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
			}

			await RoleChecker.SyncRoles(command.Member, sponsorTierRoleID);
		}
	}
}
