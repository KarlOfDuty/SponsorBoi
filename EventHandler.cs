using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using SponsorBoi.Commands;

namespace SponsorBoi;

public static class EventHandler
{
	public static Task OnReady(DiscordClient client, ReadyEventArgs e)
	{
		Logger.Log("Client is ready to process events.");

		// Checking activity type
		if (!Enum.TryParse(Config.presenceType, true, out ActivityType activityType))
		{
			Console.WriteLine("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
			activityType = ActivityType.Playing;
		}

		client.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), UserStatus.Online);
		return Task.CompletedTask;
	}

	public static Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs e)
	{
		Logger.Log("Guild available: " + e.Guild.Name);

		IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

		foreach ((ulong roleID, DiscordRole role) in roles)
		{
			Logger.Log(role.Name.PadRight(40, '.') + roleID);
		}
		return Task.CompletedTask;
	}

	public static Task OnClientError(DiscordClient client, ClientErrorEventArgs e)
	{
		Logger.Error("Exception occured:\n" + e.Exception);
		return Task.CompletedTask;
	}

	public static async Task OnGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
	{
		if (e.Guild.Id != Config.serverID) return;

		// Check if user has registered their Github account
		if (!Database.TryGetSponsor(e.Member.Id, out Database.SponsorEntry sponsorEntry)) return;

		// Check if user is active sponsor
		List <Github.Sponsor> sponsors = await Github.GetSponsors();
		Github.Sponsor sponsor = sponsors.FirstOrDefault(s => s.sponsor.id == sponsorEntry.githubID);
		if (sponsor == null) return;

		// Check if tier is registered in the config
		if (!Config.TryGetTierRole(sponsor.dollarAmount, out ulong roleID)) return;

		// Assign role
		DiscordRole role = e.Guild.GetRole(roleID);
		Logger.Log(Utils.FullName(e.Member) + " (" + e.Member.Id + ") were given back the role '" + role.Name + "' on rejoin. ");
		await e.Member.GrantRoleAsync(role);
	}

	internal static async Task OnCommandError(SlashCommandsExtension commandSystem, SlashCommandErrorEventArgs e)
	{
		switch (e.Exception)
		{
			case SlashExecutionChecksFailedException checksFailedException:
			{
				foreach (SlashCheckBaseAttribute attr in checksFailedException.FailedChecks)
				{
					await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = ParseFailedCheck(attr)
					});
				}
				return;
			}

			case BadRequestException ex:
				Logger.Error("Command exception occured:\n" + e.Exception);
				Logger.Error("JSON Message: " + ex.JsonMessage);
				return;

			default:
			{
				Logger.Error("Exception occured: " + e.Exception.GetType() + ": " + e.Exception);
				await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Internal error occured, please report this to the developer."
				});
				return;
			}
		}
	}

	internal static async Task OnComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
	{
		try
		{
			switch (e.Interaction.Data.ComponentType)
			{
				case ComponentType.Button:
					switch (e.Id)
					{
						case "sponsorboi_checkissuebutton":
							await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, (await LinkCommand.AttemptUserLink(e.Guild, e.User, false)).AsEphemeral());
							return;
						case "sponsorboi_standalonelinkbutton":
							await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, (await LinkCommand.AttemptUserLink(e.Guild, e.User, true)).AsEphemeral());
							return;
						case "right":
							return;
						case "left":
							return;
						case "rightskip":
							return;
						case "leftskip":
							return;
						case "stop":
							return;
						default:
							Logger.Warn("Unknown button press received! '" + e.Id + "'");
							return;
					}
				case ComponentType.StringSelect:
					Logger.Warn("Unknown selection box option received! '" + e.Id + "'");
					return;
				case ComponentType.ActionRow:
					Logger.Warn("Unknown action row received! '" + e.Id + "'");
					return;
				case ComponentType.FormInput:
					Logger.Warn("Unknown form input received! '" + e.Id + "'");
					return;
				default:
					Logger.Warn("Unknown interaction type received! '" + e.Interaction.Data.ComponentType + "'");
					break;
			}
		}
		catch (DiscordException ex)
		{
			Logger.Error("Interaction Exception occurred: " + ex);
			Logger.Error("JsomMessage: " + ex.JsonMessage);
		}
		catch (Exception ex)
		{
			Logger.Error("Interaction Exception occured: " + ex.GetType() + ": " + ex);
		}
	}

	private static string ParseFailedCheck(SlashCheckBaseAttribute attr)
	{
		return attr switch
		{
			SlashRequireDirectMessageAttribute => "This command can only be used in direct messages!",
			SlashRequireOwnerAttribute => "Only the server owner can use that command!",
			SlashRequirePermissionsAttribute => "You don't have permission to do that!",
			SlashRequireBotPermissionsAttribute => "The bot doesn't have the required permissions to do that!",
			SlashRequireUserPermissionsAttribute => "You don't have permission to do that!",
			SlashRequireGuildAttribute => "This command has to be used in a Discord server!",
			_ => "Unknown Discord API error occured, please try again later."
		};
	}
}