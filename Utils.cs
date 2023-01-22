using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SponsorBoi
{
	public static class Utils
	{
		public static string Minify(string input)
		{
			return JObject.Parse(input).ToString(Formatting.None);
		}

		public static string[] ParseIDs(string args)
		{
			if (string.IsNullOrEmpty(args))
			{
				return Array.Empty<string>();
			}
			return args.Trim().Replace("<@!", "").Replace("<@", "").Replace(">", "").Split();
		}

		public static bool TryGetMember(this DiscordUser user, DiscordGuild guild, out DiscordMember member)
		{
			try
			{
				member = guild.GetMemberAsync(user.Id).GetAwaiter().GetResult();
				return member != null;
			}
			catch (Exception)
			{
				member = null;
				return false;
			}
		}

		public static async void SyncUserRoles(DiscordGuild guild, DiscordUser user)
		{
			List<Github.Sponsor> sponsors = await Github.GetSponsors();
			ulong sponsorTierRoleID = 0;

			if (!Database.TryGetSponsor(user.Id, out Database.SponsorEntry sponsorEntry))
				return;

			Github.Sponsor sponsor = sponsors.FirstOrDefault(x => x.sponsor.id == sponsorEntry.githubID);
			if (sponsor != null)
			{
				sponsorTierRoleID = Config.tierRoles.GetValueOrDefault(sponsor.dollarAmount);
			}

			if (user.TryGetMember(guild, out DiscordMember member))
			{
				await RoleChecker.SyncRoles(member, sponsorTierRoleID);
			}
		}

		public static string FullName(this DiscordUser user)
	    {
     		return user.Username + "#" + user.Discriminator;
	    }

		public static string GetIssueURL(string messageContents)
		{
			string url = "https://github.com/" + Config.ownerName + "/" + Config.repositoryName
						 + "/issues/new?body=" + messageContents.Replace(' ', '+');
			if (!string.IsNullOrWhiteSpace(Config.issueTitle))
			{
				url += "&title=" + Config.issueTitle.Replace(' ', '+');
			}

			if (!string.IsNullOrWhiteSpace(Config.issueLabel))
			{
				url += "&label=" + Config.issueLabel.Replace(' ', '+');
			}
			return url;
		}
	}
}
