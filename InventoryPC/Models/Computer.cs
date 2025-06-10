using System.Collections.Generic;

namespace InventoryPC.Models
{
    public class Computer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? User { get; set; }
        public string? Branch { get; set; }
        public string? Office { get; set; }
        public string? InventoryNumber { get; set; }
        public string? WindowsVersion { get; set; }
        public string? ActivationStatus { get; set; }
        public string? LicenseExpiry { get; set; }
        public string? IPAddress { get; set; }
        public string? MACAddress { get; set; }
        public string? SSID { get; set; }
        public string? Printers { get; set; }
        public string? Processor { get; set; }
        public string? Motherboard { get; set; }
        public string? BIOSVersion { get; set; }
        public string? VideoCard { get; set; }
        public string? VideoCardMemory { get; set; }
        public string? VideoCardDriver { get; set; }
        public string? Disks { get; set; }
        public string? Monitors { get; set; }
        public string? Mouse { get; set; }
        public string? Keyboard { get; set; }
        public string? OfficeStatus { get; set; }
        public string? OfficeLicenseName { get; set; }
        public string? AntivirusName { get; set; }
        public string? AntivirusVersion { get; set; }
        public string? AntivirusLicenseExpiry { get; set; }
        public string? Memory { get; set; }
        public string? SubnetMask { get; set; }
        public string? Gateway { get; set; }
        public string? DNSServers { get; set; }
        public string? LastChecked { get; set; }
        public List<AppInfo> InstalledApps { get; set; } = new List<AppInfo>(); // Новое поле

        public void UpdateFromCollectedData(Computer collectedData)
        {
            WindowsVersion = collectedData.WindowsVersion;
            ActivationStatus = collectedData.ActivationStatus;
            LicenseExpiry = collectedData.LicenseExpiry;
            IPAddress = collectedData.IPAddress;
            MACAddress = collectedData.MACAddress;
            SSID = collectedData.SSID;
            Printers = collectedData.Printers;
            Processor = collectedData.Processor;
            Motherboard = collectedData.Motherboard;
            BIOSVersion = collectedData.BIOSVersion;
            VideoCard = collectedData.VideoCard;
            VideoCardMemory = collectedData.VideoCardMemory;
            VideoCardDriver = collectedData.VideoCardDriver;
            Disks = collectedData.Disks;
            Monitors = collectedData.Monitors;
            Mouse = collectedData.Mouse;
            Keyboard = collectedData.Keyboard;
            OfficeStatus = collectedData.OfficeStatus;
            OfficeLicenseName = collectedData.OfficeLicenseName;
            AntivirusName = collectedData.AntivirusName;
            AntivirusVersion = collectedData.AntivirusVersion;
            AntivirusLicenseExpiry = collectedData.AntivirusLicenseExpiry;
            Memory = collectedData.Memory;
            SubnetMask = collectedData.SubnetMask;
            Gateway = collectedData.Gateway;
            DNSServers = collectedData.DNSServers;
            LastChecked = collectedData.LastChecked;
            InstalledApps = collectedData.InstalledApps; // Обновление приложений
        }
    }
}