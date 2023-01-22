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
		(DiscordEmbed embed, List<DiscordComponent> buttons) = await PromptOrSyncUser(command.Guild, command.User);
		DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder().AddEmbed(embed);

		if (buttons.Count != 0)
		{
			builder.AddComponents(buttons);
		}

		await command.CreateResponseAsync(builder.AsEphemeral());
	}

	internal static async Task<(DiscordEmbed, List<DiscordComponent>)> PromptOrSyncUser(DiscordGuild guild, DiscordUser user)
	{
		if (Database.TryGetSponsor(user.Id, out Database.SponsorEntry _))
		{
			return (new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "The Discord account " + user.Mention + " is already linked to a Github account.\n\n"
				            + "Either use the /unlink command or ask an admin to help you if you want to switch accounts."
			}, new List<DiscordComponent>());
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
			return (new DiscordEmbedBuilder
			{
				Color = DiscordColor.Cyan,
				Title = "Sync Github Sponsors tier",
				Description = "You will verify the Github account is yours by posting your Discord ID in an issue.\n\n"
							+ "Simply follow these steps and the rest is done automatically:\n"
							+ "**1.** Press 'Submit Discord ID'\n"
							+ "**2.** Press 'Submit new issue' on Github\n"
							+ "**3.** Press 'Finish Link'\n"
							+ "**4.** You may now close or delete the Github issue\n"
			}, new List<DiscordComponent>
			{
				new DiscordLinkButtonComponent(Utils.GetIssueURL("Discord ID: " + user.Id), "Submit Discord ID"),
				new DiscordButtonComponent(ButtonStyle.Primary, "sponsorboi_checkissuebutton", "Finish Link")
			});
		}

		if (Database.TryGetSponsor(githubAccount.id, out Database.SponsorEntry existingSponsor))
		{
			return (new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "The Github account '" + githubAccount.name + "' is already linked to <@" + existingSponsor.discordID + ">"
			}, new List<DiscordComponent>());
		}

		if (!Database.TryAddSponsor(new Database.SponsorEntry { discordID = user.Id, githubID = githubAccount.id }))
		{
			return (new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error occured when writing sponsor to database."
			}, new List<DiscordComponent>());
		}

		Utils.SyncUserRoles(guild, user);

		return (new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "The Github account '" + githubAccount.name + "' is now linked to " + user.Mention + "\n\n"
						  + "You may now close or delete the Github issue."
		}, new List<DiscordComponent>());
	}

	internal static async Task OnButtonPressed(DiscordInteraction interaction)
	{
		if (Database.TryGetSponsor(interaction.User.Id, out Database.SponsorEntry _))
		{
			await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "The Discord account " + interaction.User.Mention + " is already linked to a Github account.\n\n"
							  + "Either use the /unlink command or ask an admin to help you if you want to switch accounts."
			}).AsEphemeral());
			return;
		}

		Github.Account githubAccount = null;
		List<Github.Issue> issues = await Github.GetIssues();
		foreach (Github.Issue issue in issues)
		{
			if (issue.description.Contains(interaction.User.Id.ToString()))
			{
				githubAccount = issue.author;
				break;
			}
		}

		if (githubAccount == null)
		{
			await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Couldn't find an open Github issue containing your Discord ID."
			}).AsEphemeral());
			return;
		}

		if (Database.TryGetSponsor(githubAccount.id, out Database.SponsorEntry existingSponsor))
		{
			await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "The Github account '" + githubAccount.name + "' is already linked to <@" + existingSponsor.discordID + ">"
			}).AsEphemeral());
			return;
		}

		if (!Database.TryAddSponsor(new Database.SponsorEntry { discordID = interaction.User.Id, githubID = githubAccount.id }))
		{
			await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error occured when writing sponsor to database."
			}).AsEphemeral());
			return;
		}

		await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "The Github account '" + githubAccount.name + "' is now linked to " + interaction.User.Mention + "\n\n"
						  + "You may now close or delete the Github issue."
		}).AsEphemeral());

		Utils.SyncUserRoles(interaction.Guild, interaction.User);
	}
}
