using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands
{
	public class AdminUnlinkCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[SlashCommand("adminunlink", "Unlinks a user from their Github account")]
		private async Task ExecuteCommand(InteractionContext command, [Option("User", "User to unlink.")] DiscordUser user)
		{
			if (user.TryGetMember(command.Guild, out DiscordMember member))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The Discord account " + member.Mention + " doesn't seem to be a member of this server."
                }, true);
				return;
			}

			if (!Database.TryGetSponsor(user.Id, out Database.SponsorEntry _))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This account is not linked."
				}, true);
				return;
			}

			if (!Database.TryRemoveSponsor(user.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured when attempting to remove sponsor from database."
				}, true);
				return;
			}

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Link removed."
			}, true);

			await RoleChecker.SyncRoles(member, 0);
		}
	}
}
