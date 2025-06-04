using InventoryPC.Models;
using InventoryPC.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace InventoryPC.ViewModels
{
    public class FirstRunViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private readonly DataService _dataService = new DataService();
        private readonly string _logPath = @"C:\Inventory\log.txt";
        private string? _office;
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

        public ICommand SaveCommand { get; }
        public ICommand SkipCommand { get; }

        public FirstRunViewModel()
        {
            _computer = new Computer
            {
                Name = System.Environment.MachineName ?? "Unknown",
                User = System.Environment.UserName ?? "Unknown"
            };
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            SkipCommand = new AsyncRelayCommand(SkipAsync);
        }

        private async Task SaveAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Office))
                {
                    _computer.Office = Office;
                    IsLoading = true;
                    ProgressValue = 0;
                    // Собираем данные
                    var progress = new Progress<int>(value => ProgressValue = value);
                    var collectedData = await _dataService.CollectDataAsync(progress);
                    _computer.UpdateFromCollectedData(collectedData);
                    await _dbService.SaveComputerAsync(_computer);
                    Log($"Saved Office: {_computer.Office} and full data for PC: {_computer.Name}");
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
                IsLoading = true;
                ProgressValue = 0;
                // Собираем данные
                var progress = new Progress<int>(value => ProgressValue = value);
                var collectedData = await _dataService.CollectDataAsync(progress);
                _computer.UpdateFromCollectedData(collectedData);
                await _dbService.SaveComputerAsync(_computer);
                Log($"Skipped Office, set to 'Не определён' and saved full data for PC: {_computer.Name}");
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
                System.IO.File.AppendAllText(_logPath, $"{System.DateTime.Now}: {message}\n", System.Text.Encoding.UTF8);
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