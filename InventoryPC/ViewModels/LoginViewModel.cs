﻿using InventoryPC.Services;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        public ICommand NavigateToFirstRunCommand { get; } // Новая команда

        public LoginViewModel()
        {
            _authService = new AuthService(new DatabaseService());
            LoginCommand = new RelayCommand<PasswordBox>(async (pb) => await LoginAsync(pb));
            GuestLoginCommand = new AsyncRelayCommand(GuestLoginAsync);
            NavigateToFirstRunCommand = new AsyncRelayCommand(NavigateToFirstRunAsync); // Инициализация новой команды
        }

        private async Task LoginAsync(PasswordBox passwordBox)
        {
            if (string.IsNullOrWhiteSpace(Login) || passwordBox == null || string.IsNullOrEmpty(passwordBox.Password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log($"Login attempt failed: Empty login or password. Login: {Login}");
                return;
            }

            // Тест хэша
            string testHash = _authService.TestHash(passwordBox.Password);
            Log($"Test hash for password {passwordBox.Password}: {testHash}");

            Log($"Login attempt: Login: {Login}, Password: {passwordBox.Password}");
            var user = await _authService.AuthenticateAsync(Login, passwordBox.Password);
            if (user != null)
            {
                App.CurrentUser = user;
                Log($"Login successful: Login: {Login}, Role: {user.Role}");
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

        private async Task NavigateToFirstRunAsync()
        {
            Log($"Navigating to FirstRunPage, App.CurrentUser: {App.CurrentUser?.Login ?? "null"}, Role: {App.CurrentUser?.Role ?? "null"}");
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new System.Uri("Views/FirstRunPage.xaml", UriKind.Relative));
                Log("Navigation to FirstRunPage successful");
            }
            else
            {
                Log("Navigation failed: MainWindow is not available");
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