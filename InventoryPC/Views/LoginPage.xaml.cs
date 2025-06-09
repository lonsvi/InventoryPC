using System.Windows.Controls;
using InventoryPC.ViewModels;

namespace InventoryPC.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
            DataContext = new LoginViewModel();
        }
    }
}