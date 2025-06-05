using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InventoryPC.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Gray;

            string? status = value.ToString();
            if (status == "Activated" || status == "Licensed")
                return Brushes.Green;

            if (status == "Not Activated" || status == "Not Licensed" || status == "Office Not Installed" || status == "None")
                return Brushes.Red;

            // Проверка сроков лицензий
            if (DateTime.TryParse(status, out DateTime expiry) && expiry <= DateTime.Now.AddDays(30))
                return expiry <= DateTime.Now ? Brushes.Red : Brushes.Yellow;

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}