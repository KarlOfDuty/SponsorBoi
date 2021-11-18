using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

namespace SponsorBoi
{
	internal static class Github
	{
		public class Sponsor
		{
			public string username;
			public uint dollarAmount;
		}

		public class Issue
		{
			public string title;
			public string description;
			public string author;
		}

		static List<Sponsor> sponsorCache = new List<Sponsor>();

		static ICompiledQuery<IEnumerable<Sponsor>> sponsorQuery = new Query()
			.Viewer
			.SponsorshipsAsMaintainer()
			.AllPages().Select(x => new Sponsor
			{
				username = x.Sponsor.Login,
				dollarAmount = (uint)x.Tier.MonthlyPriceInDollars
			})
			.Compile();

		static Octokit.GraphQL.ProductHeaderValue productInformation = new Octokit.GraphQL.ProductHeaderValue(SponsorBoi.APPLICATION_NAME, SponsorBoi.GetVersion());
		static Octokit.GraphQL.Connection connection = null;

		public static void Initialize()
		{
			connection = new Octokit.GraphQL.Connection(productInformation, Config.githubToken);
		}

		public static async Task<List<Sponsor>> GetSponsors() // Needs 'read:user' permissions
		{
			try
			{
				return (await connection.Run(sponsorQuery)).ToList();
			} 
			catch (HttpRequestException e)
			{
				LogHTTPRequestException(e);
				return new List<Sponsor>();
			}
		}

		public static async Task<List<Issue>> GetIssues() // Needs 'read:user' permissions
		{
			try
			{
				ICompiledQuery<IEnumerable<Issue>> issueQuery = new Query()
					.Viewer
					.Repository("SponsorBoi")
					.Issues(
						first: 3,
						after: null,
						last: null,
						before: null,
						filterBy: null,
						labels: null,
						orderBy: new IssueOrder { Field = IssueOrderField.CreatedAt, Direction = OrderDirection.Desc },
						states: null)
					.Nodes
					.Select(i => new Issue
					{
						title = i.Title,
						description = i.BodyText,
						author = i.Author.Login
					})
					.Compile();

				return (await connection.Run(issueQuery)).ToList();
			}
			catch (HttpRequestException e)
			{
				LogHTTPRequestException(e);
				return new List<Issue>();
			}
		}

		private static void LogHTTPRequestException(HttpRequestException e)
		{
			switch (e.StatusCode)
			{
				case System.Net.HttpStatusCode.BadGateway:
					Logger.Error(LogID.Github, "Could not connect to Github API (Bad Gateway)");
					break;

				case System.Net.HttpStatusCode.Unauthorized:
				case System.Net.HttpStatusCode.Forbidden:
					Logger.Error(LogID.Github, "Github refused request, make sure your personal access token is valid and has the right permissions.");
					break;
				default:
					Logger.Error(LogID.Github, "Unknown error occured requesting from the Github API: " + e.Message);
					break;
			}
		}

	}
}
