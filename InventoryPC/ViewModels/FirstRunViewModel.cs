using System;
using System.Threading.Tasks;
using System.Windows;
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
        public AsyncRelayCommand ReturnToLoginCommand { get; }

        public FirstRunViewModel()
        {
            _computer = new Computer
            {
                Name = Environment.MachineName ?? "Unknown",
                User = Environment.UserName ?? "Unknown"
            };
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            SkipCommand = new AsyncRelayCommand(SkipAsyncMethod);
            ReturnToLoginCommand = new AsyncRelayCommand(ReturnToLoginAsync);
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
                Log($"Starting SaveAsync, App.CurrentUser: {App.CurrentUser?.Login ?? "null"}, Role: {App.CurrentUser?.Role ?? "null"}");
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
                    Log($"Saved Office: {_computer.Office}, InventoryNumber: {_computer.InventoryNumber} for PC: {_computer.Name}");

                    // Устанавливаем гостя, если пользователь не авторизован
                    if (App.CurrentUser == null)
                    {
                        App.CurrentUser = new Models.User { Login = "Guest", Role = "User" };
                        Log("Set App.CurrentUser to Guest with Role: User");
                    }

                    Log($"Navigating to MainPage, App.CurrentUser: {App.CurrentUser?.Login}, Role: {App.CurrentUser?.Role}");
                    NavigateToMainPage();
                }
                else
                {
                    Log($"SaveAsync: Office is empty, no save performed");
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

        private async Task SkipAsyncMethod()
        {
            try
            {
                Log($"Starting SkipAsyncMethod, App.CurrentUser: {App.CurrentUser?.Login ?? "null"}, Role: {App.CurrentUser?.Role ?? "null"}");
                _computer.Office = "Не определён";
                _computer.InventoryNumber = InventoryNumber ?? "Не указан";
                IsLoading = true;
                ProgressValue = 0;
                var progress = new Progress<int>(value => ProgressValue = value);
                var collectedData = await _dataService.CollectDataAsync(progress);
                _computer.UpdateFromCollectedData(collectedData);
                await _dbService.SaveComputerAsync(_computer);
                Log($"Skipped Office, set to 'Не определён', InventoryNumber: {_computer.InventoryNumber} for PC: {_computer.Name}");

                // Устанавливаем гостя, если пользователь не авторизован
                if (App.CurrentUser == null)
                {
                    App.CurrentUser = new Models.User { Login = "Guest", Role = "User" };
                    Log("Set App.CurrentUser to Guest with Role: User");
                }

                Log($"Navigating to MainPage, App.CurrentUser: {App.CurrentUser?.Login}, Role: {App.CurrentUser?.Role}");
                NavigateToMainPage();
            }
            catch (Exception ex)
            {
                Log($"Error in SkipAsyncMethod: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReturnToLoginAsync()
        {
            try
            {
                Log("Navigating back to LoginPage from FirstRunPage.");
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.MainFrame.Navigate(new System.Uri("Views/LoginPage.xaml", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                Log($"Error in ReturnToLoginAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void NavigateToMainPage()
        {
            try
            {
                Log($"Attempting navigation to MainPage.xaml");
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.MainFrame.Navigate(new System.Uri("Views/MainPage.xaml", UriKind.Relative));
                    Log($"Navigation to MainPage.xaml successful");
                }
                else
                {
                    Log($"Navigation failed: MainWindow is not available");
                }
            }
            catch (Exception ex)
            {
                Log($"Error in NavigateToMainPage: {ex.Message}\n{ex.StackTrace}");
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