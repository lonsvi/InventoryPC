using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;
using InventoryPC.Models;
using System.Collections.Generic;

namespace InventoryPC.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = @"Data Source=C:\Inventory\inventory.db";
        private readonly string _logPath = @"C:\Inventory\log.txt";

        public DatabaseService()
        {
            Directory.CreateDirectory(@"C:\Inventory");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Computers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT,
                    User TEXT,
                    Branch TEXT,
                    Office TEXT,
                    WindowsVersion TEXT,
                    ActivationStatus TEXT,
                    LicenseExpiry TEXT,
                    IPAddress TEXT,
                    MACAddress TEXT,
                    Processor TEXT,
                    Monitors TEXT,
                    Mouse TEXT,
                    Keyboard TEXT,
                    OfficeStatus TEXT,
                    OfficeLicenseName TEXT,
                    Memory TEXT,
                    SubnetMask TEXT,
                    Gateway TEXT,
                    DNSServers TEXT,
                    LastChecked TEXT,
                    Printers TEXT,
                    AntivirusName TEXT,
                    AntivirusVersion TEXT,
                    AntivirusLicenseExpiry TEXT,
                    Motherboard TEXT,
                    BIOSVersion TEXT,
                    VideoCard TEXT,
                    VideoCardMemory TEXT,
                    VideoCardDriver TEXT,
                    Disks TEXT,
                    SSID TEXT,
                    InventoryNumber TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS idx_computers_name ON Computers(Name);";
            command.ExecuteNonQuery();
        }

        public async Task SaveComputerAsync(Computer computer)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Проверяем, существует ли запись с таким Name
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT Id FROM Computers WHERE Name = $name";
                checkCommand.Parameters.AddWithValue("$name", computer.Name ?? (object)DBNull.Value);
                var existingId = await checkCommand.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Обновляем существующую запись
                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE Computers SET
                            User = $user,
                            Branch = $branch,
                            Office = $office,
                            WindowsVersion = $windowsVersion,
                            ActivationStatus = $activationStatus,
                            LicenseExpiry = $licenseExpiry,
                            IPAddress = $ipAddress,
                            MACAddress = $macAddress,
                            Processor = $processor,
                            Monitors = $monitors,
                            Mouse = $mouse,
                            Keyboard = $keyboard,
                            OfficeStatus = $officeStatus,
                            OfficeLicenseName = $officeLicenseName,
                            Memory = $memory,
                            SubnetMask = $subnetMask,
                            Gateway = $gateway,
                            DNSServers = $dnsServers,
                            LastChecked = $lastChecked,
                            Printers = $printers,
                            AntivirusName = $antivirusName,
                            AntivirusVersion = $antivirusVersion,
                            AntivirusLicenseExpiry = $antivirusLicenseExpiry,
                            Motherboard = $motherboard,
                            BIOSVersion = $biosVersion,
                            VideoCard = $videoCard,
                            VideoCardMemory = $videoCardMemory,
                            VideoCardDriver = $videoCardDriver,
                            Disks = $disks,
                            SSID = $ssid,
                            InventoryNumber = $inventoryNumber
                        WHERE Id = $id";

                    updateCommand.Parameters.AddWithValue("$id", existingId);
                    updateCommand.Parameters.AddWithValue("$user", computer.User ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$branch", computer.Branch ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$office", computer.Office ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$windowsVersion", computer.WindowsVersion ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$activationStatus", computer.ActivationStatus ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$licenseExpiry", computer.LicenseExpiry ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$ipAddress", computer.IPAddress ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$macAddress", computer.MACAddress ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$processor", computer.Processor ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$monitors", computer.Monitors ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$mouse", computer.Mouse ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$keyboard", computer.Keyboard ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$officeStatus", computer.OfficeStatus ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$officeLicenseName", computer.OfficeLicenseName ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$memory", computer.Memory ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$subnetMask", computer.SubnetMask ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$gateway", computer.Gateway ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$dnsServers", computer.DNSServers ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$lastChecked", computer.LastChecked ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$printers", computer.Printers ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$antivirusName", computer.AntivirusName ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$antivirusVersion", computer.AntivirusVersion ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$antivirusLicenseExpiry", computer.AntivirusLicenseExpiry ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$motherboard", computer.Motherboard ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$biosVersion", computer.BIOSVersion ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$videoCard", computer.VideoCard ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$videoCardMemory", computer.VideoCardMemory ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$videoCardDriver", computer.VideoCardDriver ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$disks", computer.Disks ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$ssid", computer.SSID ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("$inventoryNumber", computer.InventoryNumber ?? (object)DBNull.Value);

                    await updateCommand.ExecuteNonQueryAsync();
                    Log($"Updated computer with Id={existingId}, Name={computer.Name}");
                }
                else
                {
                    // Вставляем новую запись
                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO Computers (
                            Name, User, Branch, Office, WindowsVersion, ActivationStatus, LicenseExpiry, 
                            IPAddress, MACAddress, Processor, Monitors, Mouse, Keyboard, OfficeStatus, 
                            OfficeLicenseName, Memory, SubnetMask, Gateway, DNSServers, LastChecked, Printers,
                            AntivirusName, AntivirusVersion, AntivirusLicenseExpiry, Motherboard, BIOSVersion,
                            VideoCard, VideoCardMemory, VideoCardDriver, Disks, SSID, InventoryNumber
                        ) VALUES (
                            $name, $user, $branch, $office, $windowsVersion, $activationStatus, $licenseExpiry, 
                            $ipAddress, $macAddress, $processor, $monitors, $mouse, $keyboard, $officeStatus, 
                            $officeLicenseName, $memory, $subnetMask, $gateway, $dnsServers, $lastChecked, $printers,
                            $antivirusName, $antivirusVersion, $antivirusLicenseExpiry, $motherboard, $biosVersion,
                            $videoCard, $videoCardMemory, $videoCardDriver, $disks, $ssid, $inventoryNumber
                        )";

                    insertCommand.Parameters.AddWithValue("$name", computer.Name ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$user", computer.User ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$branch", computer.Branch ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$office", computer.Office ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$windowsVersion", computer.WindowsVersion ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$activationStatus", computer.ActivationStatus ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$licenseExpiry", computer.LicenseExpiry ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$ipAddress", computer.IPAddress ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$macAddress", computer.MACAddress ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$processor", computer.Processor ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$monitors", computer.Monitors ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$mouse", computer.Mouse ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$keyboard", computer.Keyboard ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$officeStatus", computer.OfficeStatus ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$officeLicenseName", computer.OfficeLicenseName ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$memory", computer.Memory ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$subnetMask", computer.SubnetMask ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$gateway", computer.Gateway ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$dnsServers", computer.DNSServers ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$lastChecked", computer.LastChecked ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$printers", computer.Printers ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$antivirusName", computer.AntivirusName ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$antivirusVersion", computer.AntivirusVersion ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$antivirusLicenseExpiry", computer.AntivirusLicenseExpiry ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$motherboard", computer.Motherboard ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$biosVersion", computer.BIOSVersion ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$videoCard", computer.VideoCard ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$videoCardMemory", computer.VideoCardMemory ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$videoCardDriver", computer.VideoCardDriver ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$disks", computer.Disks ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$ssid", computer.SSID ?? (object)DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$inventoryNumber", computer.InventoryNumber ?? (object)DBNull.Value);

                    await insertCommand.ExecuteNonQueryAsync();
                    Log($"Inserted new computer: Name={computer.Name}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error in SaveComputerAsync: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<Computer>> GetComputersAsync()
        {
            var computers = new List<Computer>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Computers";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                computers.Add(new Computer
                {
                    Id = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    User = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Branch = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Office = reader.IsDBNull(4) ? null : reader.GetString(4),
                    WindowsVersion = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ActivationStatus = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LicenseExpiry = reader.IsDBNull(7) ? null : reader.GetString(7),
                    IPAddress = reader.IsDBNull(8) ? null : reader.GetString(8),
                    MACAddress = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Processor = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Monitors = reader.IsDBNull(11) ? null : reader.GetString(11),
                    Mouse = reader.IsDBNull(12) ? null : reader.GetString(12),
                    Keyboard = reader.IsDBNull(13) ? null : reader.GetString(13),
                    OfficeStatus = reader.IsDBNull(14) ? null : reader.GetString(14),
                    OfficeLicenseName = reader.IsDBNull(15) ? null : reader.GetString(15),
                    Memory = reader.IsDBNull(16) ? null : reader.GetString(16),
                    SubnetMask = reader.IsDBNull(17) ? null : reader.GetString(17),
                    Gateway = reader.IsDBNull(18) ? null : reader.GetString(18),
                    DNSServers = reader.IsDBNull(19) ? null : reader.GetString(19),
                    LastChecked = reader.IsDBNull(20) ? null : reader.GetString(20),
                    Printers = reader.IsDBNull(21) ? null : reader.GetString(21),
                    AntivirusName = reader.IsDBNull(22) ? null : reader.GetString(22),
                    AntivirusVersion = reader.IsDBNull(23) ? null : reader.GetString(23),
                    AntivirusLicenseExpiry = reader.IsDBNull(24) ? null : reader.GetString(24),
                    Motherboard = reader.IsDBNull(25) ? null : reader.GetString(25),
                    BIOSVersion = reader.IsDBNull(26) ? null : reader.GetString(26),
                    VideoCard = reader.IsDBNull(27) ? null : reader.GetString(27),
                    VideoCardMemory = reader.IsDBNull(28) ? null : reader.GetString(28),
                    VideoCardDriver = reader.IsDBNull(29) ? null : reader.GetString(29),
                    Disks = reader.IsDBNull(30) ? null : reader.GetString(30),
                    SSID = reader.IsDBNull(31) ? null : reader.GetString(31),
                    InventoryNumber = reader.IsDBNull(32) ? null : reader.GetString(32)
                });
            }
            return computers;
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n", System.Text.Encoding.UTF8);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }
    }
}