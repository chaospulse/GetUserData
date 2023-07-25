using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace UserData
{
	//class for json structure
	public class PC_Information
	{
		public string ProcessorName { get; set; }
		public string ProcessorManufacturer { get; set; }
		public string ProcessorDescription { get; set; }

		public string VideoCardName { get; set; }
		public string VideoCardVideoProcessor { get; set; }
		public string VideoCardDriverVersion { get; set; }
		public string VideoCardAdapterRAM { get; set; }

		public string CDROMDriveName { get; set; }
		public string CDROMDriveLetter { get; set; }

		public string HardDiskCaption { get; set; }
		public string HardDiskSize { get; set; }
	}
	public class PC
	{
		public static async Task<PC_Information> SendPCInfo()
		{
			PC_Information pc = new PC_Information();
			
			pc.ProcessorName = AppendHardwareInfo("Win32_Processor", "Name");
			pc.ProcessorManufacturer = AppendHardwareInfo("Win32_Processor", "Manufacturer");
			pc.ProcessorDescription = AppendHardwareInfo("Win32_Processor", "Description");

			pc.VideoCardName = AppendHardwareInfo("Win32_VideoController", "Name");
			pc.VideoCardVideoProcessor = AppendHardwareInfo("Win32_VideoController", "VideoProcessor");
			pc.VideoCardDriverVersion = AppendHardwareInfo("Win32_VideoController", "DriverVersion");
			pc.VideoCardAdapterRAM = AppendHardwareInfo("Win32_VideoController", "AdapterRAM");

			if (!String.IsNullOrEmpty(AppendHardwareInfo("Win32_CDROMDrive", "Name")))
			{
				pc.CDROMDriveName = AppendHardwareInfo("Win32_CDROMDrive", "Name");
				pc.CDROMDriveLetter = AppendHardwareInfo("Win32_CDROMDrive", "Drive");
			}
			if (!String.IsNullOrEmpty(AppendHardwareInfo("Win32_DiskDrive", "Caption")))
			{
				pc.HardDiskCaption = AppendHardwareInfo("Win32_DiskDrive", "Caption");
				pc.HardDiskSize = AppendHardwareInfo("Win32_DiskDrive", "Size");
			}
			return pc;
		}
		
		private static string AppendHardwareInfo(string WIN32_Class, string ClassItemField)
		{
			string Component_Info = "";
			List<string> result = new List<string>();
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM " + WIN32_Class);
			try
			{
				foreach (ManagementObject obj in searcher.Get())
				{
					result.Add(obj[ClassItemField].ToString().Trim());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			if (result.Count > 0)
			{
				if (result.Count == 2)
				{
					for (int i = 0; i < result.Count; ++i)
						Component_Info += result[i] + " ";
					Component_Info = Component_Info.Trim();
				}
				else
					for (int i = 0; i < result.Count; ++i)
						Component_Info = result[i];
			}
			return Component_Info;
		}
		//public static async Task<string> GetPCInfo()
		//{
		//	StringBuilder sb = new StringBuilder();
		//	sb.AppendLine("==================================================\n\n\t\tPC Information\n\n==================================================\n");

		//	sb.AppendLine("Процессор:");
		//	sb.Append(AppendHardwareInfo("Win32_Processor", "Name"));
		//	sb.Append(AppendHardwareInfo("Win32_Processor", "Manufacturer"));
		//	sb.Append(AppendHardwareInfo("Win32_Processor", "Description"));
		//	sb.AppendLine();

		//	sb.AppendLine("Видеокарта:");
		//	sb.AppendLine(AppendHardwareInfo("Win32_VideoController", "Name"));
		//	sb.AppendLine(AppendHardwareInfo("Win32_VideoController", "VideoProcessor"));
		//	sb.AppendLine(AppendHardwareInfo("Win32_VideoController", "DriverVersion"));
		//	sb.AppendLine(AppendHardwareInfo("Win32_VideoController", "AdapterRAM"));

		//	if (!String.IsNullOrEmpty(AppendHardwareInfo("Win32_CDROMDrive", "Name")))
		//	{
		//		sb.AppendLine("Название дисковода:");
		//		sb.AppendLine(AppendHardwareInfo("Win32_CDROMDrive", "Name"));
		//		sb.AppendLine(AppendHardwareInfo("Win32_CDROMDrive", "Drive"));
		//		sb.AppendLine();
		//	}
		//	if (!String.IsNullOrEmpty(AppendHardwareInfo("Win32_DiskDrive", "Caption")))
		//	{
		//		sb.AppendLine("Жесткий диск:");
		//		sb.AppendLine(AppendHardwareInfo("Win32_DiskDrive", "Caption"));
		//		sb.Append(AppendHardwareInfo("Win32_DiskDrive", "Size"));
		//	}
		//	return sb.ToString();
		//}

	}
}
