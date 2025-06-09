using InventoryPC.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;

namespace InventoryPC.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly string _logPath = @"C:\Inventory\log.txt";
        private string _login = string.Empty;

        public string Login
        {
            get => _login;
            set
            {
                _login = value;
                OnPropertyChanged(nameof(Login));
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand GuestLoginCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthService(new DatabaseService());
            LoginCommand = new RelayCommand<PasswordBox>(async (pb) => await LoginAsync(pb));
            GuestLoginCommand = new AsyncRelayCommand(GuestLoginAsync);
        }

        private async Task LoginAsync(PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(Login) || passwordBox == null || string.IsNullOrEmpty(passwordBox.Password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log("Login attempt failed: Empty login or password.");
                return;
            }

            var user = await _authService.AuthenticateAsync(Login, passwordBox.Password);
            if (user != null)
            {
                App.CurrentUser = user;
                NavigateToMainPage();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Log($"Login attempt failed for user: {Login}");
            }
        }

        private async Task GuestLoginAsync()
        {
            App.CurrentUser = new Models.User { Login = "Guest", Role = "User" };
            Log("Guest login successful.");
            NavigateToMainPage();
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