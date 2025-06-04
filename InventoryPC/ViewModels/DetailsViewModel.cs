using System.ComponentModel;
using System.Windows.Input;
using InventoryPC.Models;
using InventoryPC.Services;

namespace InventoryPC.ViewModels
{
    public class DetailsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private Computer? _computer;

        public Computer? Computer
        {
            get => _computer;
            set
            {
                _computer = value;
                OnPropertyChanged(nameof(Computer));
            }
        }

        public ICommand NavigateBackCommand { get; }
        public ICommand SaveComputerCommand { get; }

        public DetailsViewModel()
        {
            NavigateBackCommand = new RelayCommand<object?>(_ => NavigateBack());
            SaveComputerCommand = new AsyncRelayCommand(SaveComputerAsync);
        }

        public void SetComputer(Computer? computer)
        {
            Computer = computer;
        }

        private void NavigateBack()
        {
            if (App.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new System.Uri("Views/MainPage.xaml", UriKind.Relative));
            }
        }

        private async Task SaveComputerAsync()
        {
            if (Computer != null)
            {
                await _dbService.SaveComputerAsync(Computer);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}