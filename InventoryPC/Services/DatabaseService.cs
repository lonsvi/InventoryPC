using Microsoft.Data.Sqlite;
using InventoryPC.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryPC.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = @"Data Source=C:\Inventory\inventory.db";

        public DatabaseService()
        {
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
                    Name TEXT NOT NULL,
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
                    LastChecked TEXT NOT NULL
                )";
            command.ExecuteNonQuery();
        }

        public async Task SaveComputerAsync(Computer computer)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Computers (Name, WindowsVersion, ActivationStatus, LicenseExpiry, IPAddress, MACAddress, Processor, Monitors, Mouse, Keyboard, OfficeStatus, OfficeLicenseName, Memory, SubnetMask, Gateway, DNSServers, LastChecked)
                VALUES ($name, $version, $status, $expiry, $ip, $mac, $processor, $monitors, $mouse, $keyboard, $office, $officeName, $memory, $subnet, $gateway, $dns, $checked)";
            command.Parameters.AddWithValue("$name", computer.Name ?? "");
            command.Parameters.AddWithValue("$version", computer.WindowsVersion ?? "");
            command.Parameters.AddWithValue("$status", computer.ActivationStatus ?? "");
            command.Parameters.AddWithValue("$expiry", computer.LicenseExpiry ?? "");
            command.Parameters.AddWithValue("$ip", computer.IPAddress ?? "");
            command.Parameters.AddWithValue("$mac", computer.MACAddress ?? "");
            command.Parameters.AddWithValue("$processor", computer.Processor ?? "");
            command.Parameters.AddWithValue("$monitors", computer.Monitors ?? "");
            command.Parameters.AddWithValue("$mouse", computer.Mouse ?? "");
            command.Parameters.AddWithValue("$keyboard", computer.Keyboard ?? "");
            command.Parameters.AddWithValue("$office", computer.OfficeStatus ?? "");
            command.Parameters.AddWithValue("$officeName", computer.OfficeLicenseName ?? "");
            command.Parameters.AddWithValue("$memory", computer.Memory ?? "");
            command.Parameters.AddWithValue("$subnet", computer.SubnetMask ?? "");
            command.Parameters.AddWithValue("$gateway", computer.Gateway ?? "");
            command.Parameters.AddWithValue("$dns", computer.DNSServers ?? "");
            command.Parameters.AddWithValue("$checked", computer.LastChecked ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            await command.ExecuteNonQueryAsync();
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
                    Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    WindowsVersion = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ActivationStatus = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    LicenseExpiry = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    IPAddress = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    MACAddress = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Processor = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Monitors = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    Mouse = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Keyboard = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    OfficeStatus = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    OfficeLicenseName = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    Memory = reader.IsDBNull(13) ? "" : reader.GetString(13),
                    SubnetMask = reader.IsDBNull(14) ? "" : reader.GetString(14),
                    Gateway = reader.IsDBNull(15) ? "" : reader.GetString(15),
                    DNSServers = reader.IsDBNull(16) ? "" : reader.GetString(16),
                    LastChecked = reader.IsDBNull(17) ? "" : reader.GetString(17)
                });
            }
            return computers;
        }
    }
}