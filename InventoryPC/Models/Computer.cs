using System;

namespace InventoryPC.Models
{
    public class Computer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? User { get; set; }
        public string? Branch { get; set; }
        public string? Office { get; set; }
        public string? WindowsVersion { get; set; }
        public string? ActivationStatus { get; set; }
        public string? LicenseExpiry { get; set; }
        public string? IPAddress { get; set; }
        public string? MACAddress { get; set; }
        public string? Processor { get; set; }
        public string? Monitors { get; set; }
        public string? Mouse { get; set; }
        public string? Keyboard { get; set; }
        public string? OfficeStatus { get; set; }
        public string? OfficeLicenseName { get; set; }
        public string? Memory { get; set; }
        public string? SubnetMask { get; set; }
        public string? Gateway { get; set; }
        public string? DNSServers { get; set; }
        public string? LastChecked { get; set; }
        public string? Printers { get; set; }

        public void UpdateFromCollectedData(Computer collectedData)
        {
            Branch = collectedData.Branch;
            WindowsVersion = collectedData.WindowsVersion;
            ActivationStatus = collectedData.ActivationStatus;
            LicenseExpiry = collectedData.LicenseExpiry;
            IPAddress = collectedData.IPAddress;
            MACAddress = collectedData.MACAddress;
            Processor = collectedData.Processor;
            Monitors = collectedData.Monitors;
            Mouse = collectedData.Mouse;
            Keyboard = collectedData.Keyboard;
            OfficeStatus = collectedData.OfficeStatus;
            OfficeLicenseName = collectedData.OfficeLicenseName;
            Memory = collectedData.Memory;
            SubnetMask = collectedData.SubnetMask;
            Gateway = collectedData.Gateway;
            DNSServers = collectedData.DNSServers;
            LastChecked = collectedData.LastChecked;
            Printers = collectedData.Printers;
        }
    }
}