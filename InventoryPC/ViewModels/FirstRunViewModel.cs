using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using InventoryPC.Models;
using InventoryPC.Services;
using System.ComponentModel;

namespace InventoryPC.ViewModels
{
    public class FirstRunViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private readonly DataService _dataService = new DataService();
        private readonly string _logPath = @"C:\Inventory\log.txt";
        private string? _office;
        private string? _inventoryNumber;
        private readonly Computer _computer;
        private bool _isLoading;
        private double _progressValue;

        public string? Office
        {
            get => _office;
            set
            {
                _office = value;
                OnPropertyChanged(nameof(Office));
            }
        }

        public string? InventoryNumber
        {
            get => _inventoryNumber;
            set
            {
                _inventoryNumber = value;
                OnPropertyChanged(nameof(InventoryNumber));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
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

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand SkipCommand { get; }

        public FirstRunViewModel()
        {
            _computer = new Computer
            {
                Name = Environment.MachineName ?? "Unknown",
                User = Environment.UserName ?? "Unknown"
            };
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            SkipCommand = new AsyncRelayCommand(SkipAsync);

            // Попробуем получить инвентарный номер автоматически
            InitializeInventoryNumberAsync();
        }

        private async void InitializeInventoryNumberAsync()
        {
            try
            {
                var progress = new Progress<int>(_ => { });
                var collectedData = await _dataService.CollectDataAsync(progress);
                InventoryNumber = collectedData.InventoryNumber != "Unknown" ? collectedData.InventoryNumber : "";
                Log($"Initialized inventory number: {InventoryNumber}");
            }
            catch (Exception ex)
            {
                Log($"Error initializing inventory number: {ex.Message}");
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Office))
                {
                    _computer.Office = Office;
                    _computer.InventoryNumber = InventoryNumber;
                    IsLoading = true;
                    ProgressValue = 0;
                    var progress = new Progress<int>(value => ProgressValue = value);
                    var collectedData = await _dataService.CollectDataAsync(progress);
                    _computer.UpdateFromCollectedData(collectedData);
                    await _dbService.SaveComputerAsync(_computer);
                    Log($"Saved Office: {_computer.Office}, InventoryNumber: {_computer.InventoryNumber} and full data for PC: {_computer.Name}");
                    NavigateToMainPage();
                }
                else
                {
                    Log("SaveAsync: Office is empty, no save performed");
                }
            }
            catch (Exception ex)
            {
                Log($"Error in SaveAsync: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SkipAsync()
        {
            try
            {
                _computer.Office = "Не определён";
                _computer.InventoryNumber = InventoryNumber ?? "Не указан";
                IsLoading = true;
                ProgressValue = 0;
                var progress = new Progress<int>(value => ProgressValue = value);
                var collectedData = await _dataService.CollectDataAsync(progress);
                _computer.UpdateFromCollectedData(collectedData);
                await _dbService.SaveComputerAsync(_computer);
                Log($"Skipped Office, set to 'Не определён', InventoryNumber: {_computer.InventoryNumber} and saved full data for PC: {_computer.Name}");
                NavigateToMainPage();
            }
            catch (Exception ex)
            {
                Log($"Error in SkipAsync: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToMainPage()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new System.Uri("Views/MainPage.xaml", UriKind.Relative));
            }
        }

        private void Log(string message)
        {
            try
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n", System.Text.Encoding.UTF8);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}