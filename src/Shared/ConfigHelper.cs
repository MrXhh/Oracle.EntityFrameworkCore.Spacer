using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;

namespace Oracle.EntityFrameworkCore
{
	internal class ConfigHelper
	{
		internal static List<string> configItems;

		internal static bool configItemsTraced;

		internal static object locker;

		static ConfigHelper()
		{
			configItems = new List<string>();
			configItemsTraced = false;
			locker = new object();
			configItems.Add("Machine Name : " + Environment.MachineName);
			configItems.Add("User Name : " + Environment.UserName);
			configItems.Add("OS Version : " + Environment.OSVersion);
			configItems.Add("64-bit OS : " + Environment.Is64BitOperatingSystem);
			configItems.Add("64-bit Process : " + Environment.Is64BitProcess);
			configItems.Add("ProcessID: " + Process.GetCurrentProcess().Id);
			configItems.Add("AppDomainId: " + AppDomain.CurrentDomain.Id);
			try
			{
				char[] separator = new char[2]
				{
					'/',
					'\\',
				};
				string[] array = typeof(GCSettings).GetTypeInfo().Assembly.CodeBase.Split(separator, StringSplitOptions.RemoveEmptyEntries);
				int num = Array.IndexOf(array, "Microsoft.NETCore.App");
				if (num > 0 && num < array.Length - 2)
				{
					configItems.Add(".NET Core Runtime Version: " + array[num + 1]);
				}
			}
			catch
			{
			}
			try
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
				configItems.Add("Oracle Data Provider for EF Core Driver Informational Version : " + versionInfo.ProductVersion);
			}
			catch
			{
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				configItems.Add("Assembly: " + assembly.FullName);
				assembly.GetReferencedAssemblies();
			}
		}
	}
}
