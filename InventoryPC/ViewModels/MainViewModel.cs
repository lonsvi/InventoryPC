using InventoryPC.Models;
using InventoryPC.Services;
using InventoryPC.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public MainViewModel()
        {
            Computers = new ObservableCollection<Computer>();
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            NavigateToDetailsCommand = new RelayCommand<Computer>(NavigateToDetails);
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
            await _dbService.SaveComputerAsync(computer);
            Computers = new ObservableCollection<Computer>(await _dbService.GetComputersAsync());

            IsProgressVisible = false;
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