﻿using InventoryPC.Models;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq; // Добавлено для LINQ

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
                User = Environment.UserName ?? "Unknown",
                Office = "Unknown",
                LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Printers = string.Empty
            };

            Log("Starting data collection...");
            Log($"Initial Office value: {computer.Office}");

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

            // Шаг 3: IP, MAC, маска подсети, шлюз, DNS, SSID (30%)
            progress.Report(30);
            string ipconfigOutput = await RunCommandAsync("ipconfig /all");
            Log("ipconfig output: " + Truncate(ipconfigOutput, 4000));
            computer.IPAddress = ParseIPAddress(ipconfigOutput);
            computer.MACAddress = ParseMACAddress(ipconfigOutput);
            computer.SubnetMask = ParseSubnetMask(ipconfigOutput);
            computer.Gateway = ParseGateway(ipconfigOutput);
            computer.DNSServers = ParseDNSServers(ipconfigOutput);
            string wlanOutput = await RunCommandAsync("netsh wlan show interfaces");
            Log("wlan output: " + Truncate(wlanOutput, 1000));
            computer.SSID = ParseSSID(wlanOutput);

            // Установка филиала
            computer.Branch = GetBranchFromIPAddress(computer.IPAddress);
            Log($"Parsed IP: {computer.IPAddress}, Branch: {computer.Branch}, SSID: {computer.SSID}");

            // Шаг 4: IP и MAC через PowerShell (резервный, 40%)
            progress.Report(40);
            if (computer.IPAddress == "Unknown" || computer.MACAddress == "Unknown")
            {
                string psOutput = await RunPowerShellCommandAsync("Get-NetAdapter | Where-Object {$_.Status -eq 'Up' -and ($_.Name -like '*Ethernet*' -or $_.InterfaceDescription -like '*Realtek*')} | Select-Object Name, MacAddress, @{Name='IPAddress';Expression={(Get-NetIPAddress -InterfaceAlias $_.Name -AddressFamily IPv4).IPAddress}}");
                Log("powershell netadapter output: " + Truncate(psOutput, 1000));
                if (computer.IPAddress == "Unknown")
                    computer.IPAddress = ParsePowerShellIPAddress(psOutput);
                if (computer.MACAddress == "Unknown")
                    computer.MACAddress = ParsePowerShellMACAddress(psOutput);
                computer.Branch = GetBranchFromIPAddress(computer.IPAddress);
                Log($"PowerShell IP: {computer.IPAddress}, Branch: {computer.Branch}");
            }

            // Шаг 5: Процессор, материнская плата, BIOS (50%)
            progress.Report(50);
            string cpuOutput = await RunCommandAsync("wmic cpu get Name");
            Log("wmic cpu output: " + Truncate(cpuOutput, 500));
            computer.Processor = ParseProcessor(cpuOutput);
            string mbOutput = await RunCommandAsync("wmic baseboard get Manufacturer,Product");
            Log("wmic baseboard output: " + Truncate(mbOutput, 500));
            computer.Motherboard = ParseMotherboard(mbOutput);
            string biosOutput = await RunCommandAsync("wmic bios get SMBIOSBIOSVersion");
            Log("wmic bios output: " + Truncate(biosOutput, 500));
            computer.BIOSVersion = ParseBIOSVersion(biosOutput);

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

            // Шаг 9: Microsoft Office, антивирус (90%)
            progress.Report(90);
            string officeOutput = await RunCommandAsync("cscript //nologo \"%ProgramFiles%\\Microsoft Office\\Office16\\ospp.vbs\" /dstatus 2>nul || cscript //nologo \"%ProgramFiles(x86)%\\Microsoft Office\\Office16\\ospp.vbs\" /dstatus 2>nul");
            Log("office output: " + Truncate(officeOutput, 2000));
            computer.OfficeStatus = ParseOfficeStatus(officeOutput);
            computer.OfficeLicenseName = ParseOfficeLicenseName(officeOutput);
            string avOutput = await RunPowerShellCommandAsync("Get-CimInstance -Namespace root\\SecurityCenter2 -ClassName AntiVirusProduct | Select-Object displayName,productState,timestamp | Format-List");
            Log("antivirus output: " + Truncate(avOutput, 1000));
            computer.AntivirusName = ParseAntivirusName(avOutput);
            computer.AntivirusVersion = ParseAntivirusVersion(avOutput);
            computer.AntivirusLicenseExpiry = ParseAntivirusLicenseExpiry(avOutput);

            // Шаг 10: Видеокарта, диски, инвентарный номер (95%)
            progress.Report(95);
            string videoOutput = await RunCommandAsync("wmic path Win32_VideoController get Name,AdapterRAM,DriverVersion");
            Log("wmic videocontroller output: " + Truncate(videoOutput, 1000));
            computer.VideoCard = ParseVideoCard(videoOutput);
            computer.VideoCardMemory = ParseVideoCardMemory(videoOutput);
            computer.VideoCardDriver = ParseVideoCardDriver(videoOutput);
            string diskOutput = await RunCommandAsync("wmic diskdrive get Model,InterfaceType,SerialNumber");
            Log("wmic diskdrive output: " + Truncate(diskOutput, 1000));
            computer.Disks = ParseDisks(diskOutput);
            string invOutput = await RunCommandAsync("wmic csproduct get IdentifyingNumber");
            Log("wmic csproduct output: " + Truncate(invOutput, 500));
            computer.InventoryNumber = ParseInventoryNumber(invOutput);
            computer.InstalledApps = await ParseInstalledAppsAsync(); // Добавляем парсинг приложений
            Log($"Parsed {computer.InstalledApps.Count} installed apps");

            // Шаг 11: Принтеры
            progress.Report(97);
            string printerOutput = await RunCommandAsync("wmic printer get Name,PortName");
            Log("wmic printer output: " + Truncate(printerOutput, 1000));
            computer.Printers = ParsePrinters(printerOutput);

            // Завершение
            progress.Report(100);
            computer.LastChecked = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Log($"Final Office value: {computer.Office}");
            Log("Data collection completed.");
            return computer;
        }

        private string GetBranchFromIPAddress(string? ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "Unknown")
                return "Unknown";

            try
            {
                var octets = ipAddress.Split('.');
                if (octets.Length != 4 || !int.TryParse(octets[2], out int thirdOctet))
                    return "Unknown";

                return thirdOctet switch
                {
                    0 => "Писарева",
                    1 => "Гоголя",
                    2 => "Р. Люксембург",
                    _ => $"Неизвестный филиал ({thirdOctet})"
                };
            }
            catch (Exception ex)
            {
                Log($"Error parsing IP address for branch: {ex.Message}");
                return "Unknown";
            }
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
            Regex regex = new Regex(@"(?:Имя ОС|Название ОС):\s*(.+?)(?:\r\n|$)", RegexOptions.IgnoreCase);
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
            Regex regex = new Regex(@"Адаптер Ethernet.*?:[\s\S]*?IPv4.*?:\s*([\d\.]+)(?:\s*\(Основной\))?", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            return "Unknown";
        }

        private string ParseMACAddress(string output)
        {
            Regex regex = new Regex(@"Адаптер Ethernet.*?:[\s\S]*?Физический адрес.*?:\s*([\w\-]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseSubnetMask(string output)
        {
            Regex regex = new Regex(@"Адаптер Ethernet.*?:[\s\S]*?Маска подсети.*?:\s*([\d\.]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseGateway(string output)
        {
            Regex regex = new Regex(@"Адаптер Ethernet.*?:[\s\S]*?Основной шлюз.*?:\s*([\d\.]+)?", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value?.Trim() ?? "None" : "None";
        }

        private string ParseDNSServers(string output)
        {
            Regex regex = new Regex(@"Адаптер Ethernet.*?:[\s\S]*?DNS-серверы.*?:\s*([\d\.:]+(?:\s+[\d\.:]+)*)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
            {
                string dnsList = match.Groups[1].Value.Trim();
                return string.Join(", ", dnsList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            }
            return "Unknown";
        }

        private string ParseSSID(string output)
        {
            Regex regex = new Regex(@"SSID\s*:\s*(.+?)(?:\r\n|$)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "None";
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

        private string ParseMotherboard(string output)
        {
            Regex regex = new Regex(@"Manufacturer\s+(.+?)\s+Product\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            return match.Success ? $"{match.Groups[1].Value.Trim()} {match.Groups[2].Value.Trim()}" : "Unknown";
        }

        private string ParseBIOSVersion(string output)
        {
            Regex regex = new Regex(@"SMBIOSBIOSVersion\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
        }

        private string ParseMonitors(string output)
        {
            Log($"Raw monitor output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"Name\s+(.+?)(?:\r\n|\n|$)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var matches = regex.Matches(output);
            if (matches.Count > 0)
            {
                var monitors = matches.Cast<Match>()
                    .Select(m => m.Groups[1].Value.Trim())
                    .Where(m => !string.IsNullOrEmpty(m) && !m.Contains("Generic PnP Monitor", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                string result = string.Join(", ", monitors);
                Log($"Parsed monitors: {result}");
                return string.IsNullOrEmpty(result) ? "Unknown" : result;
            }
            Log("No monitors parsed");
            return "Unknown";
        }

        private string ParseMouse(string output)
        {
            Log($"Raw mouse output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"Name\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                Log($"Parsed mouse: {result}");
                return result;
            }
            Log("No mouse parsed");
            return "Unknown";
        }

        private string ParseKeyboard(string output)
        {
            Log($"Raw keyboard output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"Name\s+(.+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                Log($"Parsed keyboard: {result}");
                return result;
            }
            Log("No keyboard parsed");
            return "Unknown";
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
            Log($"Raw office license output: {Truncate(output, 2000)}");
            Regex regex = new Regex(@"LICENSE NAME:\s*(.+?)(?:\r\n|$)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(output);
            if (matches.Count > 0)
            {
                string result = string.Join(", ", matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()));
                Log($"Parsed office license name: {result}");
                return result;
            }
            Log("Parsed office license name: Unknown");
            return "Unknown";
        }

        private string ParseAntivirusName(string output)
        {
            Log($"Raw antivirus output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"displayName\s*:\s*(.+?)(?:\r\n|\n|$)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                Log($"Parsed antivirus name: {result}");
                return result;
            }
            Log("No antivirus name parsed");
            return "None";
        }

        private string ParseAntivirusVersion(string output)
        {
            Log($"Raw antivirus version output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"productState\s*:\s*(\d+)(?:\r\n|\n|$)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                Log($"Parsed antivirus version: {result}");
                return result;
            }
            Log("No antivirus version parsed");
            return "Unknown";
        }

        private string ParseAntivirusLicenseExpiry(string output)
        {
            Log($"Raw antivirus expiry output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"timestamp\s*:\s*(.+?)(?:\r\n|\n|$)", RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            DateTime dt = DateTime.MinValue;
            if (match.Success && DateTime.TryParse(match.Groups[1].Value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                string result = dt.ToString("yyyy-MM-dd");
                Log($"Parsed antivirus expiry: {result}");
                return result;
            }
            Log("No antivirus expiry parsed");
            return "Unknown";
        }

        private string ParseVideoCard(string output)
        {
            Log($"Raw video card output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"Name\s+(.+?)(?=\s+AdapterRAM|\r\n|$)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var match = regex.Match(output);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                Log($"Parsed video card: {result}");
                return result;
            }
            Log("No video card parsed");
            return "Unknown";
        }

        private string ParseVideoCardMemory(string output)
        {
            Log($"Raw video card memory output: {Truncate(output, 1000)}");

            // Попробуем найти строку AdapterRAM и взять число после неё
            // Пример: AdapterRAM 4293918720
            Regex regex = new Regex(@"AdapterRAM\s+(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var matches = regex.Matches(output);

            if (matches.Count > 0)
            {
                // Если несколько видеокарт, берём первую с ненуловым значением
                foreach (Match match in matches)
                {
                    if (long.TryParse(match.Groups[1].Value.Trim(), out long ram) && ram > 0)
                    {
                        string result = $"{ram / 1024 / 1024} MB";
                        Log($"Parsed video card memory: {result}");
                        return result;
                    }
                }
            }

            // Альтернативный способ: ищем числа больше 128*1024*1024 (128 МБ) после AdapterRAM
            regex = new Regex(@"AdapterRAM\s*[:=]?\s*(\d{8,})", RegexOptions.IgnoreCase);
            var altMatch = regex.Match(output);
            if (altMatch.Success && long.TryParse(altMatch.Groups[1].Value.Trim(), out long altRam) && altRam > 0)
            {
                string result = $"{altRam / 1024 / 1024} MB";
                Log($"Parsed video card memory (alt): {result}");
                return result;
            }

            Log("No video card memory parsed");
            return "Unknown";
        }

        private string ParseVideoCardDriver(string output)
        {
            Log($"Raw video card driver output: {Truncate(output, 1000)}");
            Regex regex = new Regex(@"DriverVersion\s+\S+\s+(\S+)", RegexOptions.Multiline);
            var match = regex.Match(output);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                Log($"Parsed video card driver: {result}");
                return result;
            }
            Log("No video card driver parsed");
            return "Unknown";
        }

        private string ParseDisks(string output)
        {
            Log($"Raw disk output: {Truncate(output, 1000)}");
            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Найдём индексы столбцов по заголовку
            int modelIdx = -1, interfaceIdx = -1, serialIdx = -1;
            if (lines.Length > 0)
            {
                var header = lines[0];
                modelIdx = header.IndexOf("Model", StringComparison.OrdinalIgnoreCase);
                interfaceIdx = header.IndexOf("InterfaceType", StringComparison.OrdinalIgnoreCase);
                serialIdx = header.IndexOf("SerialNumber", StringComparison.OrdinalIgnoreCase);
            }

            var disks = new List<string>();
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Если строка короче, чем нужно для всех индексов, пропускаем
                if (modelIdx < 0 || interfaceIdx < 0 || serialIdx < 0 || line.Length < serialIdx)
                    continue;

                try
                {
                    string model = line.Length > modelIdx && interfaceIdx > modelIdx
                        ? line.Substring(modelIdx, interfaceIdx - modelIdx).Trim()
                        : "";
                    string iface = line.Length > interfaceIdx && serialIdx > interfaceIdx
                        ? line.Substring(interfaceIdx, serialIdx - interfaceIdx).Trim()
                        : "";
                    string serial = line.Length > serialIdx
                        ? line.Substring(serialIdx).Trim()
                        : "";

                    // Иногда wmic возвращает пустые строки, фильтруем
                    if (!string.IsNullOrWhiteSpace(model))
                        disks.Add($"{model} ({iface}, S/N: {serial})");
                }
                catch (Exception ex)
                {
                    Log($"Error parsing disk line: {line} | {ex.Message}");
                }
            }

            // Если не удалось корректно распарсить, пробуем альтернативный способ (через разделитель пробелами)
            if (disks.Count == 0 && lines.Length > 1)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = Regex.Split(line, @"\s{2,}"); // разделяем по 2+ пробелам
                    if (parts.Length >= 3)
                    {
                        string model = parts[0].Trim();
                        string iface = parts[1].Trim();
                        string serial = parts[2].Trim();
                        if (!string.IsNullOrWhiteSpace(model))
                            disks.Add($"{model} ({iface}, S/N: {serial})");
                    }
                }
            }

            string result = disks.Count > 0 ? string.Join(", ", disks) : "Unknown";
            Log($"Parsed disks: {result}");
            return result;
        }

        private string ParseInventoryNumber(string output)
        {
            Regex regex = new Regex(@"IdentifyingNumber\s+(.+?)(?:\r\n|$)", RegexOptions.Multiline);
            var match = regex.Match(output);
            string result = match.Success ? match.Groups[1].Value.Trim() : "Unknown";
            Log($"Parsed InventoryNumber: {result}");
            return result;
        }

        private string ParsePrinters(string output)
        {
            Log($"Raw printer output: {Truncate(output, 1000)}");

            // Парсим таблицу WMIC: первая строка - заголовки, далее - строки с данными
            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                Log("No printers parsed (not enough lines)");
                return "None";
            }

            // Определяем индексы столбцов по заголовку
            int nameIdx = lines[0].IndexOf("Name", StringComparison.OrdinalIgnoreCase);
            int portIdx = lines[0].IndexOf("PortName", StringComparison.OrdinalIgnoreCase);

            if (nameIdx < 0 || portIdx < 0)
            {
                Log("No printers parsed (header not found)");
                return "None";
            }

            var printers = new List<string>();
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length < portIdx) continue;

                string name = line.Substring(nameIdx, portIdx - nameIdx).Trim();
                string port = line.Substring(portIdx).Trim();

                if (string.IsNullOrWhiteSpace(name)) continue;

                string ip = port.StartsWith("IP_", StringComparison.OrdinalIgnoreCase) ? port.Substring(3) : "Local";
                printers.Add($"{name} (IP: {ip}, Status: Unknown, Inventory: Not Assigned)");
            }

            string result = printers.Count > 0 ? string.Join("; ", printers) : "None";
            Log($"Parsed printers: {result}");
            return result;
        }
        private async Task<List<AppInfo>> ParseInstalledAppsAsync()
        {
            var apps = new List<AppInfo>();
            try
            {
                Log("Starting installed apps parsing...");
                var registryPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var path in registryPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null) continue;

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey == null) continue;

                                string name = subKey.GetValue("DisplayName")?.ToString()?.Trim();
                                if (string.IsNullOrEmpty(name)) continue;

                                string installDate = subKey.GetValue("InstallDate")?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(installDate) && installDate.Length == 8)
                                {
                                    try
                                    {
                                        installDate = $"{installDate.Substring(0, 4)}-{installDate.Substring(4, 2)}-{installDate.Substring(6, 2)}";
                                    }
                                    catch
                                    {
                                        installDate = "Unknown";
                                    }
                                }
                                else
                                {
                                    installDate = "Unknown";
                                }

                                apps.Add(new AppInfo
                                {
                                    Name = name,
                                    InstallDate = installDate
                                });
                            }
                        }
                    }
                }

                apps = apps
                    .Where(a => !string.IsNullOrEmpty(a.Name))
                    .GroupBy(a => a.Name)
                    .Select(g => g.First())
                    .OrderBy(a => a.Name)
                    .ToList();

                Log($"Parsed {apps.Count} unique apps");
                return apps;
            }
            catch (Exception ex)
            {
                Log($"Error parsing installed apps: {ex.Message}\n{ex.StackTrace}");
                return apps;
            }
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