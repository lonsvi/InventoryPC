using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using InventoryPC.Models;
using InventoryPC.Services;

namespace InventoryPC.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService = new DataService();
        private readonly DatabaseService _dbService = new DatabaseService();
        private ObservableCollection<Computer> _computers;
        private double _progressValue;
        private bool _isProgressVisible;

        public ObservableCollection<Computer> Computers
        {
            get => _computers;
            set { _computers = value; OnPropertyChanged(nameof(Computers)); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(nameof(ProgressValue)); }
        }

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set { _isProgressVisible = value; OnPropertyChanged(nameof(IsProgressVisible)); }
        }

        public ICommand RefreshCommand { get; }

        public MainViewModel()
        {
            Computers = new ObservableCollection<Computer>();
            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
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
            await _dbService.SaveComputerAsync(computer);
            Computers = new ObservableCollection<Computer>(await _dbService.GetComputersAsync());

            IsProgressVisible = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public RelayCommand(Func<Task> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public async void Execute(object parameter) => await _execute();
        public event EventHandler CanExecuteChanged;
    }
}