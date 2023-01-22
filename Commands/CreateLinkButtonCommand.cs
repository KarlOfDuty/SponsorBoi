using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SponsorBoi.Commands;

public class CreateSyncButtonCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("createlinkbutton", "Creates a button for users to link their Github account")]
	private async Task ExecuteCommand(InteractionContext command, [Option("Message", "Message to display above the button.")] string messageContent = " ")
	{
		DiscordMessageBuilder builder = new DiscordMessageBuilder().WithContent(messageContent);
		builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "sponsorboi_standalonelinkbutton", "Link Github account"));

		await command.Channel.SendMessageAsync(builder);

		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Cyan,
			Description = "Done."
		}, true);
	}
}