using InventoryPC.Models;
using System.Windows;

namespace InventoryPC
{
    public partial class App : Application
    {
        public static User? CurrentUser { get; set; }
    }
}