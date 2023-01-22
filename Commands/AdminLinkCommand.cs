using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands
{
	public class AdminLinkCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("adminlink", "Links a user to a Github account")]
		private async Task ExecuteCommand(InteractionContext command,
			[Option("DiscordUser", "Discord user to link.")] DiscordUser targetUser,
			[Option("GithubName", "Github user to link.")] string githubUsername)
		{
			if (targetUser.TryGetMember(command.Guild, out DiscordMember member))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Discord account " + member.Mention + " doesn't seem to be a member of this server."
				}, true);
				return;
			}

			if (Database.TryGetSponsor(member.Id, out Database.SponsorEntry _))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Discord account " + member.Mention + " is already linked to a Github account."
				}, true);
				return;
			}

			Github.Account githubAccount = await Github.GetUserByUsername(githubUsername);
			if (githubAccount == null)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Could not find Github user '" + githubUsername + "'."
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

			if (!Database.TryAddSponsor(new Database.SponsorEntry { discordID = member.Id, githubID = githubAccount.id }))
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
				Description = "The Github account '" + githubAccount.name + "' is now linked to " + member.Mention
			}, true);

			List<Github.Sponsor> sponsors = await Github.GetSponsors();
			ulong sponsorTierRoleID = 0;

			Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == githubAccount.id);
			if (sponsor != null)
			{
				sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
			}

			await RoleChecker.SyncRoles(member, sponsorTierRoleID);
		}
	}
}
