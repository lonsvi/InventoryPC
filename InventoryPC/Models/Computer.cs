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
        public string? Printers { get; set; } // Новое поле
    }
}