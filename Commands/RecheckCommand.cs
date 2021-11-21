using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
				Description = "Running recheck on all sponsors."
			});

			Logger.Log(LogID.Discord, "Started manually triggered check of sponsors...");
			await RoleChecker.RunSponsorCheck();
			Logger.Log(LogID.Discord, "Manually triggered sponsor check finished.");
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
				Description = "Running recheck on " + member.Mention + "."
			});

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
