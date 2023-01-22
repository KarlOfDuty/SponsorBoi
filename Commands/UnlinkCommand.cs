using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands
{
	public class UnlinkCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("unlink", "Unlinks you from your Github account")]
		private async Task ExecuteCommand(InteractionContext command)
		{
			if (!Database.TryGetSponsor(command.User.Id, out Database.SponsorEntry _))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This account is not linked."
				}, true);
				return;
			}

			if (Database.TryRemoveSponsor(command.User.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Link removed."
				}, true);
				return;
			}

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error occured when attempting to remove sponsor from database."
			}, true);

			DiscordMember member;
			try
			{
				member = await command.Guild.GetMemberAsync(command.User.Id);
			}
			catch (Exception)
			{
				return;
			}

			await RoleChecker.SyncRoles(member, 0);
		}
	}
}
