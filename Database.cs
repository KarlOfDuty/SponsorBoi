using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace DiscordBot
{
	internal static class Database
	{
		private static string connectionString = "";

		public class SavedRole
		{
			public ulong userID;
			public ulong roleID;
			public DateTime time;

			public SavedRole(MySqlDataReader reader)
			{
				this.userID = reader.GetUInt64("user_id");
				this.roleID = reader.GetUInt64("role_id");
				this.time = reader.GetDateTime("time");
			}
		}

		public static void SetConnectionString(string host, int port, string database, string username, string password)
		{
			connectionString = "server=" + host +
			                   ";database=" + database +
			                   ";port=" + port +
			                   ";userid=" + username +
			                   ";password=" + password;
		}

		public static MySqlConnection GetConnection()
		{
			return new MySqlConnection(connectionString);
		}

		public static void SetupTables()
		{
			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand createTable = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS tracked_roles(" +
					"user_id BIGINT UNSIGNED NOT NULL," +
					"role_id BIGINT UNSIGNED NOT NULL," +
					"time DATETIME NOT NULL," +
					"INDEX(user_id, time))",
					c);
				c.Open();
				createTable.ExecuteNonQuery();
			}
		}

		public static bool TryAddRole(ulong userID, ulong roleID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand cmd = new MySqlCommand(@"INSERT INTO tracked_roles (user_id, role_id, time) VALUES (@user_id, @role_id, now());", c);
				cmd.Parameters.AddWithValue("@user_id", userID);
				cmd.Parameters.AddWithValue("@role_id", roleID);
				cmd.Prepare();

				return cmd.ExecuteNonQuery() > 0;
			}

		}

		public static bool TryGetRoles(ulong userID, out List<SavedRole> roles)
		{
			roles = null;
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tracked_roles WHERE user_id=@user_id", c);
				selection.Parameters.AddWithValue("@user_id", userID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				if (!results.Read())
				{
					return false;
				}

				roles = new List<SavedRole> { new SavedRole(results) };
				while (results.Read())
				{
					roles.Add(new SavedRole(results));
				}
				results.Close();
				return true;
			}
		}

		public static bool TryRemoveRoles(ulong userID)
		{
			using (MySqlConnection c = GetConnection())
			{
				c.Open();

				MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tracked_roles WHERE user_id=@user_id", c);
				deletion.Parameters.AddWithValue("@user_id", userID);
				deletion.Prepare();

				return deletion.ExecuteNonQuery() > 0;
			}
		}
	}
}
