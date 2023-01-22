using System;
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
