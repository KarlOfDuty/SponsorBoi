using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace SponsorBoi.Commands
{
	public class UnlinkCommand : BaseCommandModule
	{
		[Command("unlink")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, DiscordUser targetUser)
		{
			await ExecuteCommand(command, targetUser);
		}

		[Command("unlink")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			await ExecuteCommand(command, command.User);
		}

		private async Task ExecuteCommand(CommandContext command, DiscordUser targetUser)
		{
			if (!await Utils.VerifySelfOtherPermission(command, targetUser, "unlink")) return;

			if (!Database.TryGetSponsor(targetUser.Id, out Database.SponsorEntry _))
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This account is not linked."
				});
				return;
			}

			if (Database.TryRemoveSponsor(targetUser.Id))
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Link removed."
				});
			}
			else
			{
				await command.RespondAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when attempting to remove sponsor from database."
				});
			}


			DiscordMember member;
			try
			{
				member = await command.Guild.GetMemberAsync(targetUser.Id);
			}
			catch (Exception)
			{
				return;
			}

			await RoleChecker.SyncRoles(member, 0);
		}
	}
}
