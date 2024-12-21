using System.Management;
using System.Text;

namespace SystemRemoteService;

public class SystemInfo
{
    public static string GetSystemInformation()
    {
        StringBuilder info = new StringBuilder();

        info.AppendLine("System Information:");
        info.AppendLine($"OS: {Environment.OSVersion}");
        info.AppendLine($"Machine Name: {Environment.MachineName}");
        info.AppendLine($"User Name: {Environment.UserName}");
        info.AppendLine($"Domain Name: {Environment.UserDomainName}");
        info.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        info.AppendLine($"Processor Count: {Environment.ProcessorCount}");
        info.AppendLine($"Memory: {GetMemoryInfo()}");

        info.AppendLine("\nProcessor Information:");
        foreach (var processor in GetProcessorInfo())
        {
            info.AppendLine(processor);
        }

        info.AppendLine("\nDisk Information:");
        foreach (var disk in GetDiskInfo())
        {
            info.AppendLine(disk);
        }

        info.AppendLine("\nNetwork Information:");
        foreach (var network in GetNetworkInfo())
        {
            info.AppendLine(network);
        }

        return info.ToString();
    }

    internal static string GetMemoryInfo()
    {
        decimal totalmemory = 0;
        decimal speed = 0;
        using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed FROM Win32_PhysicalMemory"))
        {
            foreach (var item in searcher.Get())
            {
                totalmemory += Convert.ToDecimal(item["Capacity"]) / 1024 / 1024 / 1024;
                speed = Convert.ToDecimal(item["Speed"]);
            }
        }

        return $"{totalmemory}GB {speed}MHz";
    }

    private static string[] GetProcessorInfo()
    {
        var processors = new List<string>();

        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
        {
            foreach (var item in searcher.Get())
            {
                processors.Add($"Name: {item["Name"]}");
                processors.Add($"Manufacturer: {item["Manufacturer"]}");
                processors.Add($"Architecture: {item["Architecture"]}");
                processors.Add($"Cores: {item["NumberOfCores"]}");
                processors.Add($"Logical Processors: {item["NumberOfLogicalProcessors"]}");
            }
        }

        return processors.ToArray();
    }

    private static string[] GetDiskInfo()
    {
        var disks = new List<string>();

        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk"))
        {
            foreach (var item in searcher.Get())
            {
                disks.Add($"DeviceID: {item["DeviceID"]}");
                disks.Add($"Volume Name: {item["VolumeName"]}");
                disks.Add($"Drive Type: {item["DriveType"]}");
                disks.Add($"Size: {FormatSize(item["Size"])}");
                disks.Add($"Free Space: {FormatSize(item["FreeSpace"])}");
            }
        }

        return disks.ToArray();
    }

    private static string[] GetNetworkInfo()
    {
        var networks = new List<string>();

        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = true"))
        {
            foreach (var item in searcher.Get())
            {
                networks.Add($"Description: {item["Description"]}");
                networks.Add($"MAC Address: {item["MACAddress"]}");
                networks.Add($"IP Address: {string.Join(", ", (string[])item["IPAddress"])}");
                networks.Add($"Subnet Mask: {string.Join(", ", (string[])item["IPSubnet"])}");
            }
        }

        return networks.ToArray();
    }

    private static string FormatSize(object size)
    {
        if (size == null)
            return "N/A";
        long sizeInBytes = Convert.ToInt64(size);
        return $"{sizeInBytes / (1024 * 1024 * 1024)} GB";
    }
}
