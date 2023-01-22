using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands
{
	public class LinkCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("link", "Link your Github account for Sponsor syncing")]
		internal static async Task ExecuteCommand(InteractionContext command)
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
				DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
				{
					Color = DiscordColor.Cyan,
					Description = "Click [here](" + Utils.GetIssueURL("Discord ID: " + command.Member.Id) + ")"
								  + " and click submit in order to verify your account.\n\n"
								  + "Click the button below when done."
				});
				builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "sponsorboi_linkcommandbutton", "Sync Github account"));
				await command.CreateResponseAsync(builder.AsEphemeral());
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

			Utils.SyncUserRoles(command.Guild, command.User);
		}

		internal static async Task OnButtonPressed(DiscordInteraction interaction)
		{
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
}
