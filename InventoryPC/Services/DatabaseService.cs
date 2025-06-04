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
                    Printers TEXT
                )";
            command.ExecuteNonQuery();
        }

        public async Task SaveComputerAsync(Computer computer)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR REPLACE INTO Computers (
                        Name, User, Branch, Office, WindowsVersion, ActivationStatus, LicenseExpiry, 
                        IPAddress, MACAddress, Processor, Monitors, Mouse, Keyboard, OfficeStatus, 
                        OfficeLicenseName, Memory, SubnetMask, Gateway, DNSServers, LastChecked, Printers
                    ) VALUES (
                        $name, $user, $branch, $office, $windowsVersion, $activationStatus, $licenseExpiry, 
                        $ipAddress, $macAddress, $processor, $monitors, $mouse, $keyboard, $officeStatus, 
                        $officeLicenseName, $memory, $subnetMask, $gateway, $dnsServers, $lastChecked, $printers
                    )";

                command.Parameters.AddWithValue("$name", computer.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$user", computer.User ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$branch", computer.Branch ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$office", computer.Office ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$windowsVersion", computer.WindowsVersion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$activationStatus", computer.ActivationStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$licenseExpiry", computer.LicenseExpiry ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$ipAddress", computer.IPAddress ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$macAddress", computer.MACAddress ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$processor", computer.Processor ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$monitors", computer.Monitors ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$mouse", computer.Mouse ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$keyboard", computer.Keyboard ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$officeStatus", computer.OfficeStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$officeLicenseName", computer.OfficeLicenseName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$memory", computer.Memory ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$subnetMask", computer.SubnetMask ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$gateway", computer.Gateway ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$dnsServers", computer.DNSServers ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$lastChecked", computer.LastChecked ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("$printers", computer.Printers ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"C:\Inventory\log.txt", $"{DateTime.Now}: Error in SaveComputerAsync: {ex.Message}\n{ex.StackTrace}\n");
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
                    Printers = reader.IsDBNull(21) ? null : reader.GetString(21)
                });
            }
            return computers;
        }
    }
}