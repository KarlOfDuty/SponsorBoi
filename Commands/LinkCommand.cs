using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands;

public class LinkCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("link", "Link your Github account for Sponsor syncing")]
	internal static async Task ExecuteCommand(InteractionContext command)
	{
		await command.CreateResponseAsync((await AttemptUserLink(command.Guild, command.User, true)).AsEphemeral());
	}

	internal static async Task<DiscordInteractionResponseBuilder> AttemptUserLink(DiscordGuild guild, DiscordUser user, bool showHelpOnFail)
	{
		DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder();

		if (Database.TryGetSponsor(user.Id, out Database.SponsorEntry _))
		{
			return response.AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "The Discord account " + user.Mention + " is already linked to a Github account.\n\n"
							  + "Either use the /unlink command or ask an admin to help you if you want to switch accounts."
			});
		}

		Github.Account githubAccount = null;
		List<Github.Issue> issues = await Github.GetIssues();
		foreach (Github.Issue issue in issues)
		{
			if (issue.description.Contains(user.Id.ToString()))
			{
				githubAccount = issue.author;
				break;
			}
		}

		if (githubAccount == null)
		{
			if (showHelpOnFail)
			{
				return response.AddEmbed(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Cyan,
					Title = "Link Github Sponsor",
					Description = "You will verify the Github account is yours by posting your Discord ID in an issue.\n\n"
								+ "Simply follow these steps and the rest is done automatically:\n"
								+ "**1.** Press 'Submit Discord ID'\n"
								+ "**2.** Press 'Submit new issue' on Github\n"
								+ "**3.** Press 'Finish Link'\n"
								+ "**4.** You may now close or delete the Github issue\n"
				}).AddComponents(new List<DiscordComponent>
				{
					new DiscordLinkButtonComponent(Utils.GetIssueURL("Discord ID: " + user.Id), "Submit Discord ID"),
					new DiscordButtonComponent(ButtonStyle.Primary, "sponsorboi_checkissuebutton", "Finish Link")
				});
			}
			else
			{
				return response.AddEmbed(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Couldn't find an open Github issue containing your Discord ID.\n\n"
					            + "Make sure you clicked submit on the issue and try again."
				});
			}
		}

		if (Database.TryGetSponsor(githubAccount.id, out Database.SponsorEntry existingSponsor))
		{
			return response.AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "The Github account '" + githubAccount.name + "' is already linked to <@" + existingSponsor.discordID + ">"
			});
		}

		if (!Database.TryAddSponsor(new Database.SponsorEntry { discordID = user.Id, githubID = githubAccount.id }))
		{
			return response.AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error occured when writing sponsor to database."
			});
		}

		Utils.SyncUserRoles(guild, user);

		return response.AddEmbed(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "The Github account '" + githubAccount.name + "' is now linked to " + user.Mention + "\n\n"
						  + "You may now close or delete the Github issue."
		});
	}
}
