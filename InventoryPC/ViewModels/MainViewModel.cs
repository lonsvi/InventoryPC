using CsvHelper;
using InventoryPC.Models;
using InventoryPC.Services;
using InventoryPC.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InventoryPC.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService = new DataService();
        private readonly DatabaseService _dbService = new DatabaseService();
        private ObservableCollection<Computer>? _computers;
        private double _progressValue;
        private bool _isProgressVisible;
        private string? _searchText;
        private string? _selectedBranch;
        private bool _expiringLicensesOnly;
        private List<string> _branches = new List<string> { "Все филиалы", "Писарева", "Гоголя", "Р. Люксембург" };

        public string? SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterComputers();
            }
        }

        public string? SelectedBranch
        {
            get => _selectedBranch;
            set
            {
                _selectedBranch = value;
                OnPropertyChanged(nameof(SelectedBranch));
                FilterComputers();
            }
        }

        public bool ExpiringLicensesOnly
        {
            get => _expiringLicensesOnly;
            set
            {
                _expiringLicensesOnly = value;
                OnPropertyChanged(nameof(ExpiringLicensesOnly));
                FilterComputers();
            }
        }

        public List<string> Branches
        {
            get => _branches;
            set
            {
                _branches = value;
                OnPropertyChanged(nameof(Branches));
            }
        }

        private async void FilterComputers()
        {
            var computers = await _dbService.GetComputersAsync();
            var filtered = computers.AsEnumerable();

            if (!string.IsNullOrEmpty(SearchText))
            {
                filtered = filtered.Where(c =>
                    (c.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.IPAddress?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (SelectedBranch != "Все филиалы" && !string.IsNullOrEmpty(SelectedBranch))
            {
                filtered = filtered.Where(c => c.Branch == SelectedBranch);
            }

            if (ExpiringLicensesOnly)
            {
                filtered = filtered.Where(c =>
                {
                    bool hasExpiring = false;
                    if (DateTime.TryParse(c.LicenseExpiry, out DateTime winExpiry) && winExpiry <= DateTime.Now.AddDays(30))
                        hasExpiring = true;
                    if (DateTime.TryParse(c.OfficeLicenseName, out DateTime officeExpiry) && officeExpiry <= DateTime.Now.AddDays(30))
                        hasExpiring = true;
                    if (DateTime.TryParse(c.AntivirusLicenseExpiry, out DateTime avExpiry) && avExpiry <= DateTime.Now.AddDays(30))
                        hasExpiring = true;
                    return hasExpiring;
                });
            }

            Computers = new ObservableCollection<Computer>(filtered);
        }

        public ObservableCollection<Computer>? Computers
        {
            get => _computers;
            set
            {
                _computers = value;
                OnPropertyChanged(nameof(Computers));
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set
            {
                _isProgressVisible = value;
                OnPropertyChanged(nameof(IsProgressVisible));
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand NavigateToDetailsCommand { get; }
        public ICommand ExportToCsvCommand { get; }

        public MainViewModel()
        {
            Computers = new ObservableCollection<Computer>();
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            NavigateToDetailsCommand = new RelayCommand<Computer>(NavigateToDetails);
            ExportToCsvCommand = new AsyncRelayCommand(ExportToCsvAsync);
            SelectedBranch = "Все филиалы";
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            var computers = await _dbService.GetComputersAsync();
            Computers = new ObservableCollection<Computer>(computers);
        }

        private async Task RefreshAsync()
        {
            IsProgressVisible = true;
            ProgressValue = 0;

            var progress = new Progress<int>(value => ProgressValue = value);
            var computer = await _dataService.CollectDataAsync(progress);
            computer.User = Environment.UserName;

            // Сохраняем Office из существующей записи
            var currentPcName = Environment.MachineName ?? "Unknown";
            var existingComputer = (await _dbService.GetComputersAsync())
                .FirstOrDefault(c => c.Name == currentPcName);
            if (existingComputer != null && !string.IsNullOrWhiteSpace(existingComputer.Office))
            {
                computer.Office = existingComputer.Office;
            }

            await _dbService.SaveComputerAsync(computer);
            Computers = new ObservableCollection<Computer>(await _dbService.GetComputersAsync());

            IsProgressVisible = false;
        }

        private async Task ExportToCsvAsync()
        {
            try
            {
                var computers = await _dbService.GetComputersAsync();
                using (var writer = new StreamWriter(@"C:\Inventory\computers.csv", false, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(computers);
                }
                File.AppendAllText(@"C:\Inventory\log.txt", $"{DateTime.Now}: Exported {computers.Count} computers to C:\\Inventory\\computers.csv\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"C:\Inventory\log.txt", $"{DateTime.Now}: Error exporting to CSV: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private void NavigateToDetails(Computer? computer)
        {
            if (App.Current.MainWindow is MainWindow mainWindow && computer != null)
            {
                var detailsPage = new DetailsPage();
                var detailsViewModel = (DetailsViewModel)detailsPage.DataContext;
                detailsViewModel.SetComputer(computer);
                mainWindow.MainFrame.Navigate(detailsPage);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}