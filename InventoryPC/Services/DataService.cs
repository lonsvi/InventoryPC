using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using InventoryPC.Models;

namespace InventoryPC.Services
{
    public class DataService
    {
        private readonly string _logPath = @"C:\Inventory\log.txt";
        private readonly Encoding _cmdEncoding;

        public DataService()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _cmdEncoding = Encoding.GetEncoding(866); // CP866 для русской Windows
            }
            catch (Exception ex)
            {
                Log($"Error initializing CP866 encoding: {ex.Message}");
                _cmdEncoding = Encoding.UTF8;
            }
        }

        public async Task<Computer> CollectDataAsync(IProgress<int> progress)
        {
            var computer = new Computer
            {
                Name = Environment.MachineName ?? "Unknown",
                User = Environment.UserName ?? "Unknown", // Текущий пользователь
                LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Log("Starting data collection...");

            // Шаг 1: Версия Windows и ОЗУ (10%)
            progress.Report(10);
            string systemInfo = await RunCommandAsync("systeminfo");
            Log("systeminfo output: " + Truncate(systemInfo, 2000));
            computer.WindowsVersion = ParseSystemInfo(systemInfo);
            computer.Memory = ParseMemory(systemInfo);

            // Шаг 2: Статус и срок активации Windows (20%)
            progress.Report(20);
            string slmgrOutput = await RunCommandAsync("cscript //nologo %windir%\\system32\\slmgr.vbs /xpr");
            Log("slmgr output: " + Truncate(slmgrOutput, 500));
            computer.ActivationStatus = ParseActivationStatus(slmgrOutput);
            computer.LicenseExpiry = ParseLicenseExpiry(slmgrOutput);

            // Шаг 3: IP, MAC, маска подсети, шлюз, DNS (30%)
            progress.Report(30);
            string ipconfigOutput = await RunCommandAsync("ipconfig /all");
            Log("ipconfig output: " + Truncate(ipconfigOutput, 2000));
            computer.IPAddress = ParseIPAddress(ipconfigOutput);
            computer.MACAddress = ParseMACAddress(ipconfigOutput);
            computer.SubnetMask = ParseSubnetMask(ipconfigOutput);
            computer.Gateway = ParseGateway(ipconfigOutput);
            computer.DNSServers = ParseDNSServers(ipconfigOutput);

            // Шаг 4: IP и MAC через PowerShell (резервный, 40%)
            progress.Report(40);
            if (computer.IPAddress == "Unknown" || computer.MACAddress == "Unknown")
            {
                string psOutput = await RunPowerShellCommandAsync("Get-NetAdapter | Where-Object {$_.Status -eq 'Up'} | Select-Object Name, MacAddress, @{Name='IPAddress';Expression={(Get-NetIPAddress -InterfaceAlias $_.Name -AddressFamily IPv4).IPAddress}}");
                Log("powershell netadapter output: " + Truncate(psOutput, 1000));
                if (computer.IPAddress == "Unknown")
                    computer.IPAddress = ParsePowerShellIPAddress(psOutput);
                if (computer.MACAddress == "Unknown")
                    computer.MACAddress = ParsePowerShellMACAddress(psOutput);
            }

            // Шаг 5: Процессор (50%)
            progress.Report(50);
            string cpuOutput = await RunCommandAsync("wmic cpu get Name");
            Log("wmic cpu output: " + Truncate(cpuOutput, 500));
            computer.Processor = ParseProcessor(cpuOutput);

            // Шаг 6: Мониторы (60%)
            progress.Report(60);
            string monitorOutput = await RunCommandAsync("wmic path Win32_PnPEntity where \"Service='monitor'\" get Name");
            Log("wmic monitor output: " + Truncate(monitorOutput, 500));
            computer.Monitors = ParseMonitors(monitorOutput);

            // Шаг 7: Мышь (70%)
            progress.Report(70);
            string mouseOutput = await RunCommandAsync("wmic path Win32_PointingDevice get Name");
            Log("wmic mouse output: " + Truncate(mouseOutput, 500));
            computer.Mouse = ParseMouse(mouseOutput);

            // Шаг 8: Клавиатура (80%)
            progress.Report(80);
            string keyboardOutput = await RunCommandAsync("wmic path Win32_Keyboard get Name");
            Log("wmic keyboard output: " + Truncate(keyboardOutput, 500));
            computer.Keyboard = ParseKeyboard(keyboardOutput);

            // Шаг 9: Microsoft Office (90%)
            progress.Report(90);
            string officeOutput = await RunCommandAsync("cscript //nologo \"%ProgramFiles%\\Microsoft Office\\Office16\\ospp.vbs\" /dstatus 2>nul || cscript //nologo \"%ProgramFiles(x86)%\\Microsoft Office\\Office16\\ospp.vbs\" /dstatus 2>nul");
            Log("office output: " + Truncate(officeOutput, 2000));
            computer.OfficeStatus = ParseOfficeStatus(officeOutput);
            computer.OfficeLicenseName = ParseOfficeLicenseName(officeOutput);

            // Завершение (100%)
            progress.Report(100);
            Log("Data collection completed.");
            return computer;
        }

        private async Task<string> RunCommandAsync(string command)
        {
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = _cmdEncoding,
                        StandardErrorEncoding = _cmdEncoding
                    }
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                if (!string.IsNullOrEmpty(error))
                {
                    Log($"Command error: {error}");
                    return $"Error: {error}";
                }
                return output;
            }
            catch (Exception ex)
            {
                Log($"Error running command '{command}': {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private async Task<string> RunPowerShellCommandAsync(string script)
        {
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"{script}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                if (!string.IsNullOrEmpty(error))
                {
                    Log($"PowerShell error: {error}");
                    return $"Error: {error}";
                }
                return output;
            }
            catch (Exception ex)
            {
                Log($"Error running PowerShell command '{script}': {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private string ParseSystemInfo(string output)
        {
            Regex regex = new Regex(@"Имя ОС:\s*(.+?)(?:\r\n|$)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            regex = new Regex(@"OS Name:\s*(.+?)(?:\r\n|$)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseMemory(string output)
        {
            Regex regex = new Regex(@"Полный объем физической памяти:\s*([\d\s]+)\s*МБ", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value.Replace(" ", "") + " MB";
            regex = new Regex(@"Total Physical Memory:\s*([\d\s]+)\s*MB", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Replace(" ", "") + " MB" : "Unknown";
        }

        private string ParseActivationStatus(string output)
        {
            if (Regex.IsMatch(output, @"активирована|is activated|лицензия действительна|activated|permanent|Windows\(R\)", RegexOptions.IgnoreCase))
                return "Activated";
            if (Regex.IsMatch(output, @"режим уведомления|Notification mode|не активирована|not activated", RegexOptions.IgnoreCase))
                return "Not Activated";
            return "Unknown";
        }

        private string ParseLicenseExpiry(string output)
        {
            Regex regex = new Regex(@"(\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2}:\d{2})", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Permanent";
        }

        private string ParseIPAddress(string output)
        {
            Regex regex = new Regex(@"IPv4-адрес.*?:\s*([\d\.]+)\s*\(Основной\)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            regex = new Regex(@"IPv4 Address.*?:\s*([\d\.]+)\s*\(Preferred\)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseMACAddress(string output)
        {
            Regex regex = new Regex(@"Физический адрес.*?:\s*([\w\-]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            regex = new Regex(@"Physical Address.*?:\s*([\w\-]+)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseSubnetMask(string output)
        {
            Regex regex = new Regex(@"Маска подсети.*?:\s*([\d\.]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            regex = new Regex(@"Subnet Mask.*?:\s*([\d\.]+)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseGateway(string output)
        {
            Regex regex = new Regex(@"Основной шлюз.*?:\s*([\d\.]+)?", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value?.Trim() ?? "None";
            regex = new Regex(@"Default Gateway.*?:\s*([\d\.]+)?", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            return match.Success ? match.Groups[1].Value?.Trim() ?? "None" : "None";
        }

        private string ParseDNSServers(string output)
        {
            Regex regex = new Regex(@"DNS-серверы.*?:\s*([\d\.:]+(?:\s+[\d\.:]+)*)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
            {
                string dnsList = match.Groups[1].Value.Trim();
                return string.Join(", ", dnsList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            regex = new Regex(@"DNS Servers.*?:\s*([\d\.:]+(?:\s+[\d\.:]+)*)", RegexOptions.IgnoreCase);
            match = regex.Match(output);
            if (match.Success)
            {
                string dnsList = match.Groups[1].Value.Trim();
                return string.Join(", ", dnsList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            return "Unknown";
        }

        private string ParsePowerShellIPAddress(string output)
        {
            Regex regex = new Regex(@"IPAddress\s*:\s*([\d\.]+)");
            var match = regex.Match(output);
            while (match.Success)
            {
                string ip = match.Groups[1].Value.Trim();
                if (!ip.StartsWith("169.254"))
                    return ip;
                match = match.NextMatch();
            }
            return "Unknown";
        }

        private string ParsePowerShellMACAddress(string output)
        {
            Regex regex = new Regex(@"MacAddress\s*:\s*([\w\-]+)");
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseProcessor(string output)
        {
            Regex regex = new Regex(@"Name\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseMonitors(string output)
        {
            Regex regex = new Regex(@"Name\s+(.+)", RegexOptions.Multiline);
            var matches = regex.Matches(output);
            if (matches.Count > 0)
                return string.Join(", ", matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()));
            return "Unknown";
        }

        private string ParseMouse(string output)
        {
            Regex regex = new Regex(@"Name\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseKeyboard(string output)
        {
            Regex regex = new Regex(@"Name\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseOfficeStatus(string output)
        {
            if (Regex.IsMatch(output, @"LICENSE STATUS:.*LICENSED", RegexOptions.IgnoreCase))
                return "Licensed";
            if (Regex.IsMatch(output, @"LICENSE STATUS:.*OOB_GRACE", RegexOptions.IgnoreCase))
                return "Grace Period (Cracked)";
            if (Regex.IsMatch(output, @"LICENSE STATUS:.*NOT LICENSED", RegexOptions.IgnoreCase))
                return "Not Licensed";
            if (string.IsNullOrEmpty(output) || Regex.IsMatch(output, @"ERROR|не найдено|file not found", RegexOptions.IgnoreCase))
                return "Office Not Installed";
            return "Unknown";
        }

        private string ParseOfficeLicenseName(string output)
        {
            Regex regex = new Regex(@"LICENSE NAME:\s*(.+?)(?:\r\n|$)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(output);
            if (matches.Count > 0)
                return string.Join(", ", matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()));
            return "Unknown";
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n", Encoding.UTF8);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }

        private string Truncate(string text, int maxLength)
        {
            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }
    }
}