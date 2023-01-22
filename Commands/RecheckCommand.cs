using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands
{
	public class RecheckCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("recheck", "Recheck one or all users")]
		public async Task OnExecute(InteractionContext command, [Option("User", "User to recheck.")] DiscordUser user = null)
		{
			if (user == null)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Running recheck on all sponsors."
				}, true);

				Logger.Log("Started manually triggered check of sponsors...");
				await RoleChecker.RunSponsorCheck();
				Logger.Log("Manually triggered sponsor check finished.");
				return;
			}

			if (!Database.TryGetSponsor(user.Id, out Database.SponsorEntry sponsorEntry))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "That user isn't linked to a Github account."
				}, true);
				return;
			}

			if (user.TryGetMember(command.Guild, out DiscordMember member))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "The Discord account " + member.Mention + " doesn't seem to be a member of this server."
				}, true);
				return;
			}

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Running recheck on " + member.Mention + "."
			}, true);

			List<Github.Sponsor> sponsors = await Github.GetSponsors();

			ulong sponsorTierRoleID = 0;
			Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == sponsorEntry.githubID);
			if (sponsor != null)
			{
				sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
			}

			await RoleChecker.SyncRoles(member, sponsorTierRoleID);
		}
	}
}
