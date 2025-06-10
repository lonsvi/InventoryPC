using InventoryPC.Models;
using InventoryPC.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InventoryPC.ViewModels
{
    public class DetailsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private Computer? _computer;
        private string _searchText;
        private ObservableCollection<AppInfo> _filteredApps;
        private readonly string _logPath = @"C:\Inventory\log.txt";

        public DetailsViewModel()
        {
            _filteredApps = new ObservableCollection<AppInfo>();
            NavigateBackCommand = new AsyncRelayCommand(NavigateBackAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
        }

        public Computer? Computer
        {
            get => _computer;
            set
            {
                _computer = value;
                OnPropertyChanged(nameof(Computer));
                UpdateFilteredApps();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                UpdateFilteredApps();
            }
        }

        public ObservableCollection<AppInfo> FilteredApps
        {
            get => _filteredApps;
            private set
            {
                _filteredApps = value;
                OnPropertyChanged(nameof(FilteredApps));
            }
        }

        public AsyncRelayCommand NavigateBackCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }

        public void SetComputer(Computer? computer)
        {
            Computer = computer;
            Log($"Set computer: Id={computer?.Id}, Name={computer?.Name}");
        }

        private async Task NavigateBackAsync()
        {
            try
            {
                Log("Navigate back to MainPage");
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.MainFrame.Navigate(new Uri("Views/MainPage.xaml", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                Log($"Error navigating back: {ex.Message}\n{ex.StackTrace}");
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
                    MessageBox.Show("Данные успешно сохранены.");
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

        private void UpdateFilteredApps()
        {
            if (Computer?.InstalledApps == null)
            {
                FilteredApps.Clear();
                Log("UpdateFilteredApps: No apps to filter");
                return;
            }

            var filtered = Computer.InstalledApps
                .Where(app => string.IsNullOrEmpty(SearchText) ||
                              app.Name.ToLower().Contains(SearchText.ToLower()))
                .OrderBy(app => app.Name)
                .ToList();

            FilteredApps.Clear();
            foreach (var app in filtered)
            {
                FilteredApps.Add(app);
            }
            Log($"UpdateFilteredApps: Filtered {filtered.Count} apps for search '{SearchText}'");
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