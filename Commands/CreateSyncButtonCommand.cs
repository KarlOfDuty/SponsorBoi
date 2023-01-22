using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands;

public class CreateSyncButtonCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("createsyncbutton", "Creates a button for users to sync their Github account")]
	private async Task ExecuteCommand(InteractionContext command)
	{
		DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder().WithContent(" ");
		builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "sponsorboi_standalonelinkbutton", "Sync Github account"));
		await command.CreateResponseAsync(builder);
	}
}