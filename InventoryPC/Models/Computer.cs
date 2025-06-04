namespace InventoryPC.Models
{
    public class Computer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WindowsVersion { get; set; }
        public string ActivationStatus { get; set; }
        public string LicenseExpiry { get; set; }
        public string IPAddress { get; set; }
        public string MACAddress { get; set; }
        public string Processor { get; set; }
        public string Monitors { get; set; } // Список мониторов через запятую
        public string Mouse { get; set; }
        public string Keyboard { get; set; }
        public string OfficeStatus { get; set; }
        public string OfficeLicenseName { get; set; } // Полное название Office
        public string Memory { get; set; } // ОЗУ в МБ
        public string SubnetMask { get; set; }
        public string Gateway { get; set; }
        public string DNSServers { get; set; } // Список DNS через запятую
        public string LastChecked { get; set; }
    }
}