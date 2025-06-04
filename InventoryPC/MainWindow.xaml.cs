using InventoryPC.Models;
using InventoryPC.Services;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace InventoryPC
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        private readonly string _logPath = @"C:\Inventory\log.txt";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Log($"MainWindow_Loaded started for PC: {Environment.MachineName}");
                var computers = await _dbService.GetComputersAsync();
                var currentPcName = Environment.MachineName ?? "Unknown";
                var currentPc = computers.FirstOrDefault(c => c.Name == currentPcName);

                if (currentPc == null || string.IsNullOrWhiteSpace(currentPc.Office))
                {
                    Log($"Navigating to FirstRunPage: PC={currentPcName}, Office={(currentPc?.Office ?? "null")}");
                    MainFrame.Navigate(new System.Uri("Views/FirstRunPage.xaml", UriKind.Relative));
                }
                else
                {
                    Log($"Navigating to MainPage: PC={currentPcName}, Office={currentPc.Office}");
                    MainFrame.Navigate(new System.Uri("Views/MainPage.xaml", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                Log($"Error in MainWindow_Loaded: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n", Encoding.UTF8);
            }
            catch
            {
                // Игнорируем ошибки логирования
            }
        }
    }
}