using System.ComponentModel;
using System.Windows;
using System.Windows.Navigation;
using InventoryPC.Models;
using InventoryPC.Services;

namespace InventoryPC.ViewModels
{
    public class DetailsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private Computer? _computer;
        private readonly string _logPath = @"C:\Inventory\log.txt";

        public Computer? Computer
        {
            get => _computer;
            set
            {
                _computer = value;
                OnPropertyChanged(nameof(Computer));
            }
        }

        public AsyncRelayCommand NavigateBackCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }

        public DetailsViewModel()
        {
            NavigateBackCommand = new AsyncRelayCommand(NavigateBackAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        public void SetComputer(Computer? computer)
        {
            Computer = computer;
            Log($"Set computer: Id={computer?.Id}, Name={computer?.Name}");
        }

        private async Task NavigateBackAsync()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new System.Uri("Views/MainPage.xaml", UriKind.Relative));
            }
        }

        private async Task SaveAsync()
        {
            if (Computer != null)
            {
                try
                {
                    Log($"Saving computer: Id={Computer.Id}, Name={Computer.Name}, Office={Computer.Office}, InventoryNumber={Computer.InventoryNumber}");
                    await _dbService.SaveComputerAsync(Computer);
                    Log($"Saved computer: Id={Computer.Id}, Name={Computer.Name}");
                }
                catch (Exception ex)
                {
                    Log($"Error saving computer: {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Log("SaveAsync: Computer is null");
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