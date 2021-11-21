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
		public class Account
		{
			public string id = "";
			public string name = "";
		}

		public class Sponsor
		{
			public Account sponsor = new Account();
			public int dollarAmount = 0;
		}

		public class Issue
		{
			public string description = "";
			public Account author = new Account();
		}

		private static ProductHeaderValue productInformation = new ProductHeaderValue(SponsorBoi.APPLICATION_NAME, SponsorBoi.GetVersion());
		private static Connection connection = null;

		private static ICompiledQuery<IEnumerable<Sponsor>> sponsorQuery = null;
		private static ICompiledQuery<IEnumerable<Issue>> issueQuery = null;

		public static void Initialize()
		{
			sponsorQuery = new Query() // Needs 'read:org' permission (And maybe 'read:user'?) 
				.Viewer
				.SponsorshipsAsMaintainer()
				.AllPages().Select(x => new Sponsor
				{
					sponsor = x.SponsorEntity.Switch<Account>(when => when
						.User(user => new Account { id = user.Id.Value, name = user.Login })
						.Organization(org => new Account { id = org.Id.Value, name = org.Login })),
					dollarAmount = x.Tier.MonthlyPriceInDollars
				})
				.Compile();

			issueQuery = new Query() // Does not require any specific permissions
				.Viewer
				.Repository(Config.repositoryName)
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
					description = i.BodyText,
					author = i.Author.Cast<User>().Select(user => new Account { id = user.Id.Value, name = user.Login } ).Single()
				})
				.Compile();

			connection = new Connection(productInformation, Config.githubToken);
		}

		public static async Task<List<Sponsor>> GetSponsors()
		{
			try
			{
				return (await connection?.Run(sponsorQuery))?.ToList();
			} 
			catch (HttpRequestException e)
			{
				LogException(new AggregateException(e));
				return new List<Sponsor>();
			}
			catch (Octokit.GraphQL.Core.Deserializers.ResponseDeserializerException e)
			{
				LogException(new AggregateException(e));
				return new List<Sponsor>();
			}
			catch (AggregateException e)
			{
				LogException(e);
				return new List<Sponsor>();
			}
		}

		public static async Task<List<Issue>> GetIssues()
		{
			try
			{
				return (await connection?.Run(issueQuery))?.ToList();
			}
			catch (HttpRequestException e)
			{
				LogException(new AggregateException(e));
				return new List<Issue>();
			}
			catch (Octokit.GraphQL.Core.Deserializers.ResponseDeserializerException e)
			{
				LogException(new AggregateException(e));
				return new List<Issue>();
			}
			catch (AggregateException e)
			{
				LogException(e);
				return new List<Issue>();
			}
		}

		public static async Task<Account> GetUserByUsername(string username)
		{
			try
			{
				ICompiledQuery<Account> userQuery = new Query() // Does not require any specific permissions
					.User(username)
					.Select(user => new Account { id = user.Id.Value, name = user.Login })
					.Compile();

				return await connection?.Run(userQuery);
			}
			catch (HttpRequestException e)
			{
				LogException(new AggregateException(e));
				return null;
			}
			catch (Octokit.GraphQL.Core.Deserializers.ResponseDeserializerException e)
			{
				LogException(new AggregateException(e));
				return null;
			}
			catch (AggregateException e)
			{
				LogException(e);
				return null;
			}
		}

		public static async Task<Account> GetUserByID(string id)
		{
			try
			{
				ICompiledQuery<Account> userQuery = new Query() // Does not require any specific permissions
					.Node(new ID(id)).Cast<User>()
					.Select(user => new Account { id = user.Id.Value, name = user.Login })
					.Compile();

				return await connection?.Run(userQuery);
			}
			catch (HttpRequestException e)
			{
				LogException(new AggregateException(e));
				return null;
			}
			catch (Octokit.GraphQL.Core.Deserializers.ResponseDeserializerException e)
			{
				LogException(new AggregateException(e));
				return null;
			}
			catch (AggregateException e)
			{
				LogException(e);
				return null;
			}
		}

		private static void LogException(AggregateException e)
		{
			e.Handle(ex =>
			{
				switch (ex)
				{
					case Octokit.GraphQL.Core.Deserializers.ResponseDeserializerException:
						Logger.Error(LogID.Github, "Error occured when reading Github response:\n" + ex);
						return true;
					case HttpRequestException except:
						LogHTTPRequestException(except);
						return true;
					default:
						return false;
				}
			});
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
