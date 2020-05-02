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
			public ulong discordUser;
			public string githubUser;

			public SponsorEntry(MySqlDataReader reader)
			{
				this.discordUser = reader.GetUInt64("discord_user");
				this.githubUser = reader.GetString("github_user");
			}
		}

		public static void Initialize()
		{
			connectionString = "server="    + Config.hostName +
			                   ";database=" + Config.database +
			                   ";port="     + Config.port +
			                   ";userid="   + Config.username +
			                   ";password=" + Config.password;

			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand createTable = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS sponsors(" +
					"discord_user BIGINT UNSIGNED NOT NULL UNIQUE," +
					"github_user VARCHAR(256) NOT NULL UNIQUE)",
					c);
				c.Open();
				createTable.ExecuteNonQuery();
				createTable.Dispose();
			}
		}

		private static MySqlConnection GetConnection()
		{
			return new MySqlConnection(connectionString);
		}
		
		public static bool TryAddSponsor(SponsorEntry sponsorEntry)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO sponsors (discord_user, github_user) VALUES (@discord_user, @github_user);", c);
				cmd.Parameters.AddWithValue("@discord_user", sponsorEntry.discordUser);
				cmd.Parameters.AddWithValue("@github_user", sponsorEntry.githubUser);
				cmd.Prepare();

				int output = cmd.ExecuteNonQuery();
				cmd.Dispose();

				return output > 0;
			}
		}

		public static bool TryGetSponsor(string githubUser, out SponsorEntry sponsorEntry)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM sponsors WHERE github_user=@github_user", c);
				selection.Parameters.AddWithValue("@github_user", githubUser);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();
				selection.Dispose();

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
		}

		public static bool TryGetSponsor(ulong userID, out SponsorEntry sponsorEntry)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM sponsors WHERE discord_user=@discord_user", c);
				selection.Parameters.AddWithValue("@discord_user", userID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();
				selection.Dispose();

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
		}

		public static bool TryRemoveSponsor(ulong userID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM sponsors WHERE discord_user=@discord_user", c);
				deletion.Parameters.AddWithValue("@discord_user", userID);
				deletion.Prepare();

				int output = deletion.ExecuteNonQuery();
				deletion.Dispose();

				return output > 0;
			}
		}
	}
}
