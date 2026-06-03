using System.Globalization;

namespace ErizosCF.Services
{
    public class DifficultyColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int difficulty)
            {
                if (difficulty < 1000) return "#4CAF50";      // Verde - Fácil
                if (difficulty < 1200) return "#8BC34A";      // Verde claro
                if (difficulty < 1400) return "#CDDC39";      // Lima
                if (difficulty < 1600) return "#FFC107";      // Amarillo - Medio
                if (difficulty < 1800) return "#FF9800";      // Naranja
                if (difficulty < 2000) return "#FF5722";      // Naranja oscuro
                if (difficulty < 2400) return "#F44336";      // Rojo
                if (difficulty < 2800) return "#E91E63";      // Rosa
                return "#9C27B0";                              // Púrpura - Leyenda
            }
            return "#A0A0A0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}