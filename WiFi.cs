using NativeWifi;
using static NativeWifi.Wlan;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using static NativeWifi.WlanClient;
using System.IO;

namespace UserData
{
	public class WiFi_Information
	{
		public string InterfaceName { get; set; }
		public string InterfaceType { get; set; }
		public string InterfaceId { get; set; }
		public string MacAddress { get; set; }
		public string IPv6Address { get; set; }
		public string IPv4Address { get; set; }
		public string IPv4SubnetMask { get; set; }
		public string WiFiName { get; set; }
		public string ConnectionStatus { get; set; }
		public string WiFiSignal { get; set; }
		public string WiFiSignalQuality { get; set; }
		public string WiFiSignalLevel { get; set; }
		public string WiFiSignalLevelQuality { get; set; }
		public string WiFiChannel { get; set; }
		public string AccessPointMacAddress { get; set; }
		public string WiFiEncryptionType { get; set; }
		public string WiFiSecurityKey { get; set; }
		public string AuthenticationTypeWiFi { get; set; }
		public string WiFiPhysicalLayerType { get; set; }
		public string WiFiPassword { get; set; }
	}
	public class WiFi_Profile
	{
		public string ProfileName { get; set; }
		public string SecurityKey { get; set; }
		public string SecurityKeyType { get; set; }
	}

	public class WiFi_Data
	{
		public WiFi_Information CurrentWiFiProfile { get; set; }
		public List<WiFi_Profile> OtherWifiProfiles { get; set; }
	}
	public class WiFi
	{
		public static async Task<WiFi_Information> GetWiFiInfo()
		{
			WiFi_Information wifi = new WiFi_Information();

			NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in interfaces)
			{
				//CURRENT CONNECTION
				if (adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
				{
					wifi.InterfaceName = $"{adapter.Name}";
					wifi.InterfaceType = $"{adapter.NetworkInterfaceType}";
					wifi.InterfaceId = $"{adapter.Id}";
					wifi.MacAddress = $"{adapter.GetPhysicalAddress()}";

					// GET IPV4 & IPV6
					IPInterfaceProperties ipProperties = adapter.GetIPProperties();
					if (ipProperties != null)
					{
						foreach (UnicastIPAddressInformation ipInfo in ipProperties.UnicastAddresses)
						{
							if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // -> IPv4
							{
								wifi.IPv4Address = $"{ipInfo.Address}";
								wifi.IPv4SubnetMask = $"{ipInfo.IPv4Mask}";
							}
							else if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) // -> IPv6
							{
								wifi.IPv6Address = $"{ipInfo.Address}";
							}
						}
					}
					// GET INFO ABOUT CURRENT WIFI CONNECTION 
					WlanClient client = new WlanClient();
					foreach (WlanClient.WlanInterface wlanInterface in client.Interfaces)
					{
						//WIFI NAME
						string ssid_name = wlanInterface.CurrentConnection.profileName;
						wifi.WiFiName = ssid_name;

						var isConnected = wlanInterface.CurrentConnection.isState;
						wifi.ConnectionStatus = $"{isConnected}";

						//WIFI SIGNAL
						var signalStrength = wlanInterface.CurrentConnection.wlanAssociationAttributes.wlanSignalQuality;
						string signal_Quality =
							signalStrength <= 20
							? "Плохой"
							: signalStrength <= 40
							? "Слабый"
							: signalStrength <= 60
							? "Умеренный"
							: signalStrength <= 80
							? "Хороший"
							: "Отличный";
						wifi.WiFiSignal = $"{signalStrength} (dBm)";
						wifi.WiFiSignalLevel = $"{signal_Quality}";

						//WIFI SIGNAL
						Wlan.WlanBssEntry[] wlanBssEntries = wlanInterface.GetNetworkBssList();
						if (wlanBssEntries.Length > 0)
						{
							var network = wlanBssEntries.FirstOrDefault();
							signal_Quality =
								network.rssi >= -30
								? "Отличный"
								: network.rssi >= -50
								? "Хороший"
								: network.rssi >= -70
								? "Умеренный"
								: network.rssi >= -85
								? "Плохой"
								: "Очень плохой";
							wifi.WiFiSignalLevel = $"{network.rssi}  (dBm)";
							wifi.WiFiSignalLevelQuality = $"{signal_Quality}";
						}

						//WIFI CHANEL
						WlanClient.WlanInterface wlanInterfaceManaged = new WlanClient().Interfaces.FirstOrDefault(w => w.InterfaceDescription == adapter.Description);
						foreach (Wlan.WlanBssEntry entry in wlanBssEntries)
						{
							string entrySsidName = Encoding.ASCII.GetString(entry.dot11Ssid.SSID, 0, (int)entry.dot11Ssid.SSIDLength).Trim('\0');
							var channel = entry.chCenterFrequency / 1000;
							wifi.WiFiChannel = $"{channel}";
							break;
						}

						//MAC-ADDRESS
						byte[] bssid = wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11Bssid;
						string bssidString = string.Join(":", bssid.Select(b => b.ToString("X2")));
						wifi.AccessPointMacAddress = $"{bssidString}";

						//
						Dot11CipherAlgorithm cipherAlgorithm = wlanInterface.CurrentConnection.wlanSecurityAttributes.dot11CipherAlgorithm;
						wifi.WiFiEncryptionType = $"{cipherAlgorithm}";

						//SECURITY KEY TYPE OF CURRENT CONNECTION
						string profileXml = wlanInterface.GetProfileXml(ssid_name);
						string securityKeyType = GetWordBetween(profileXml, "<authentication>", "</authentication>");
						wifi.WiFiSecurityKey = $"{securityKeyType}";

						//AUTHENTICATION TYPE
						Wlan.Dot11AuthAlgorithm authAlgorithm = wlanInterface.CurrentConnection.wlanSecurityAttributes.dot11AuthAlgorithm;
						wifi.AuthenticationTypeWiFi = $"{authAlgorithm}";

						Wlan.Dot11PhyType phyType = wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11PhyType;
						wifi.WiFiPhysicalLayerType = $"{phyType}";

						// PASSWORD OF CURRENT CONNECTION
						string password = GetPassword(ssid_name);
						wifi.WiFiPassword = $"{password}";
						break;
					}
				}
			}
			return wifi;
		}
		public static async Task<List<WiFi_Profile>> GetOtherWiFiProfilesInfo()
		{
			List<WiFi_Profile> otherWiFiProfiles = new List<WiFi_Profile>();

			WlanClient client = new WlanClient();
			foreach (WlanClient.WlanInterface wlanInterface in client.Interfaces)
			{
				Wlan.WlanProfileInfo[] profiles = wlanInterface.GetProfiles();
				foreach (Wlan.WlanProfileInfo profileInfo in profiles)
				{
					string profileXml = wlanInterface.GetProfileXml(profileInfo.profileName);
					WiFi_Profile wifiProfile = new WiFi_Profile
					{
						//ssid name
						ProfileName = profileInfo.profileName,
						//password
						SecurityKey = GetPassword(profileInfo.profileName),
						//security key
						SecurityKeyType = GetWordBetween(profileXml, "<authentication>", "</authentication>")
					};
					otherWiFiProfiles.Add(wifiProfile);
				}
			}
			return otherWiFiProfiles;
		}
		public static string GetWordBetween(string input, string startWord, string endWord)
		{
			int startIndex = input.IndexOf(startWord);
			if (startIndex < 0)
				return string.Empty;

			startIndex += startWord.Length;

			int endIndex = input.IndexOf(endWord, startIndex);
			if (endIndex < 0)
				return string.Empty;

			return input.Substring(startIndex, endIndex - startIndex).Trim();
		}
		private static string GetPassword(string ssidName)
		{
			Process process = new Process();
			process.StartInfo.FileName = "netsh";
			process.StartInfo.Arguments = $"wlan show profile name=\"{ssidName}\" key=clear";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.Start();
			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			int passwordStartIndex = output.IndexOf("Key Content");
			if (passwordStartIndex != -1)
			{
				int passwordEndIndex = output.IndexOf("\r\n", passwordStartIndex);
				if (passwordEndIndex != -1)
				{
					string passwordLine = output.Substring(passwordStartIndex, passwordEndIndex - passwordStartIndex);
					string[] parts = passwordLine.Split(':');
					if (parts.Length == 2)
					{
						string password = parts[1].Trim();
						return password;
					}
				}
			}
			return "";
		}
	}
}
