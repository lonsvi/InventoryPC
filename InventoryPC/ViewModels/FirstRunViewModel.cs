using InventoryPC.Models;
using InventoryPC.Services;
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
        private readonly string _logPath = @"C:\Inventory\log.txt";
        private string? _office;
        private readonly Computer _computer;

        public string? Office
        {
            get => _office;
            set
            {
                _office = value;
                OnPropertyChanged(nameof(Office));
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
                    await _dbService.SaveComputerAsync(_computer);
                    Log($"Saved Office: {_computer.Office} for PC: {_computer.Name}");
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
        }

        private async Task SkipAsync()
        {
            try
            {
                _computer.Office = null; // Пустое значение для Office
                await _dbService.SaveComputerAsync(_computer);
                Log($"Skipped Office for PC: {_computer.Name}");
                NavigateToMainPage();
            }
            catch (Exception ex)
            {
                Log($"Error in SkipAsync: {ex.Message}\n{ex.StackTrace}");
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