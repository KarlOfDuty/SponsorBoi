using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace SponsorBoi
{
	internal static class Database
	{
		private static string connectionString = "";

		public struct SponsorEntry
		{
			public ulong discordID;
			public string githubID;

			public SponsorEntry(MySqlDataReader reader)
			{
				this.discordID = reader.GetUInt64("discord_id");
				this.githubID = reader.GetString("github_id");
			}
		}

		public static void Initialize()
		{
			connectionString = "server="    + Config.hostName +
			                   ";database=" + Config.database +
			                   ";port="     + Config.port +
			                   ";userid="   + Config.username +
			                   ";password=" + Config.password;

			using MySqlConnection c = GetConnection();
			c.Open();
			MySqlCommand createTable = new MySqlCommand(
				"CREATE TABLE IF NOT EXISTS sponsors(" +
				"discord_id BIGINT UNSIGNED NOT NULL UNIQUE," +
				"github_id VARCHAR(256) NOT NULL UNIQUE)",
				c);
			createTable.ExecuteNonQuery();
		}

		private static MySqlConnection GetConnection()
		{
			return new MySqlConnection(connectionString);
		}
		
		public static bool TryAddSponsor(SponsorEntry sponsorEntry)
		{
			using MySqlConnection c = GetConnection();
			c.Open();

			MySqlCommand cmd = new MySqlCommand(@"INSERT INTO sponsors (discord_id, github_id) VALUES (@discord_id, @github_id);", c);
			cmd.Parameters.AddWithValue("@discord_id", sponsorEntry.discordID);
			cmd.Parameters.AddWithValue("@github_id", sponsorEntry.githubID);

			int output = cmd.ExecuteNonQuery();

			return output > 0;
		}

		public static List<SponsorEntry> GetAllSponsors()
		{
			using MySqlConnection c = GetConnection();
			c.Open();

			MySqlCommand selection = new MySqlCommand(@"SELECT * FROM sponsors", c);
			MySqlDataReader results = selection.ExecuteReader();

			List<SponsorEntry> sponsors = new List<SponsorEntry>();
			while (results.Read())
			{
				sponsors.Add(new SponsorEntry(results));
			}

			results.Close();
			return sponsors;
		}

		public static bool TryGetSponsor(string githubID, out SponsorEntry sponsorEntry)
		{
			using MySqlConnection c = GetConnection();
			c.Open();

			MySqlCommand selection = new MySqlCommand(@"SELECT * FROM sponsors WHERE github_id=@github_id", c);
			selection.Parameters.AddWithValue("@github_id", githubID);
			selection.Prepare();
			MySqlDataReader results = selection.ExecuteReader();

			if (!results.Read())
			{
				sponsorEntry = new SponsorEntry();
				results.Close();
				return false;
			}

			sponsorEntry = new SponsorEntry(results);
			results.Close();
			return true;
		}

		public static bool TryGetSponsor(ulong discordID, out SponsorEntry sponsorEntry)
		{
			using MySqlConnection c = GetConnection();
			c.Open();

			MySqlCommand selection = new MySqlCommand(@"SELECT * FROM sponsors WHERE discord_id=@discord_id", c);
			selection.Parameters.AddWithValue("@discord_id", discordID);
			selection.Prepare();
			MySqlDataReader results = selection.ExecuteReader();

			if (!results.Read())
			{
				sponsorEntry = new SponsorEntry();
				results.Close();
				return false;
			}

			sponsorEntry = new SponsorEntry(results);
			results.Close();
			return true;
		}

		public static bool TryRemoveSponsor(ulong userID)
		{
			using MySqlConnection c = GetConnection();
			c.Open();

			MySqlCommand deletion = new MySqlCommand(@"DELETE FROM sponsors WHERE discord_id=@discord_id", c);
			deletion.Parameters.AddWithValue("@discord_id", userID);
			deletion.Prepare();

			int output = deletion.ExecuteNonQuery();

			return output > 0;
		}
	}
}
