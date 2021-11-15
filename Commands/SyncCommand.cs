using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Bcpg;

namespace SponsorBoi.Commands
{
	public class SyncCommand
	{
		[Command("sync")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if github user already exists

			// Put user in database, replace if duplicate discord id

			ulong userID = 0;
			string[] parsedArgs = Utils.ParseIDs(command.RawArgumentString);

			DiscordUser targetUser = await Utils.VerifyTargetUser(command, "sync", 1);
			if (targetUser == null) return;

			if (Database.TryGetSponsor(targetUser.Id, out Database.SponsorEntry _))
			{

			}

			// Outdated code below
			DiscordMember member;
			try
			{
				member = await command.Guild.GetMemberAsync(userID);
			}
			catch (Exception)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not find user on this server)"
				};
				await command.RespondAsync(error);
				return;
			}

			DiscordEmbed message = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = member.Mention + " was added to staff."
			};
			await command.RespondAsync(message);
		}
	}
}
