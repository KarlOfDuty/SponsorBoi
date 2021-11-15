using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SponsorBoi
{
	internal static class Github
	{
		private static HttpClient client;

		private static readonly Uri apiAdress = new Uri("https://api.github.com/graphql");

		private static readonly string githubUserRegex = "/^[a-z\\d](?:[a-z\\d]|-(?=[a-z\\d])){0,38}$/i";

		private static readonly StringContent getSponsorsQuery = new StringContent(Utils.Minify(
	@"
		{
			""query"":""query
			{ 
				viewer
				{ 
					sponsorshipsAsMaintainer(first:100)
					{
						nodes
						{
							tier
							{
								monthlyPriceInDollars
							}
							sponsor
							{
								login
							}
						}
					}
				}
			}""
		}"));

		public class Sponsor
		{
			public string username;
			public uint dollarAmount;

			public Sponsor(JToken sponsor)
			{
				username = sponsor.SelectToken("sponsor.login").Value<string>();
				dollarAmount = sponsor.SelectToken("tier.monthlyPriceInDollars").Value<uint>();
			}
		}

		public static void Initialize()
		{
			client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Config.githubToken);
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(SponsorBoi.APPLICATION_NAME, SponsorBoi.GetVersion()));
		}

		public static async Task<List<Sponsor>> GetSponsors()
		{
			HttpResponseMessage result = await client.PostAsync(apiAdress, getSponsorsQuery);
			string resultContent = await result.Content.ReadAsStringAsync();

			JObject jo = JObject.Parse(resultContent);

			string str = jo.ToString();

			if (jo.TryGetValue("message", out JToken jt))
			{
				if (jt.Value<string>() == "Bad credentials")
				{
					Logger.Error(LogID.Github, "Github refused your personal access token");
					return new List<Sponsor>();
				}
			}

			List<JToken> sponsors = jo.SelectToken("data.viewer.sponsorshipsAsMaintainer.nodes").Value<JArray>().ToList();

			List<Sponsor> output = new List<Sponsor>();
			foreach (JToken sponsor in sponsors)
			{
				output.Add(new Sponsor(sponsor));
			}

			return output;
		}
	}
}
