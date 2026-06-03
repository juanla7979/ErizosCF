using SkiaSharp;
using System.Collections.Generic;

namespace ErizosCF.Services
{
    public static class TagColorService
    {
        // Diccionario estático que mantiene el color asignado a cada tag
        private static readonly Dictionary<string, SKColor> _tagColors = new();

        // Paleta base de colores predefinidos
        private static readonly string[] _colorPalette = new[]
        {
            "#FF6B6B", // Rojo coral
            "#4ECDC4", // Turquesa
            "#FFE66D", // Amarillo
            "#95E77E", // Verde manzana
            "#FF9F4A", // Naranja
            "#B983FF", // Púrpura
            "#FF6B9D", // Rosa
            "#6C91B2", // Azul grisáceo
            "#F4A261", // Terracota
            "#2A9D8F", // Verde azulado
            "#E76F51", // Naranja quemado
            "#9C89B8", // Lavanda
            "#88D498", // Verde menta
            "#FFB347", // Naranja mandarina
            "#6A4E9B", // Morado
            "#00A6A6", // Verde azulado oscuro
            "#EF476F", // Rosa intenso
            "#FFD166", // Amarillo mostaza
            "#06D6A0", // Verde esmeralda
            "#C77DFF", // Lila
            "#FF9E8F", // Salmón
            "#7B2CBF", // Morado profundo
            "#52B788", // Verde bosque
            "#E9C46A", // Amarillo dorado
            "#E63946", // Rojo intenso
            "#4895EF"  // Azul brillante
        };

        private static int _currentColorIndex = 0;

        /// <summary>
        /// Obtiene o asigna un color consistente para un tag específico
        /// </summary>
        public static SKColor GetColorForTag(string tag)
        {
            // Si ya existe un color asignado, lo devuelve
            if (_tagColors.ContainsKey(tag))
                return _tagColors[tag];

            // Si no existe, asigna un nuevo color de la paleta
            var color = SKColor.Parse(_colorPalette[_currentColorIndex % _colorPalette.Length]);
            _tagColors[tag] = color;
            _currentColorIndex++;

            return color;
        }

        /// <summary>
        /// Resetea la asignación de colores (útil si quieres empezar de nuevo)
        /// </summary>
        public static void ResetColors()
        {
            _tagColors.Clear();
            _currentColorIndex = 0;
        }

        /// <summary>
        /// Obtiene una lista de colores para múltiples tags (preservando consistencia)
        /// </summary>
        public static List<SKColor> GetColorsForTags(List<string> tags)
        {
            var colors = new List<SKColor>();
            foreach (var tag in tags)
            {
                colors.Add(GetColorForTag(tag));
            }
            return colors;
        }
    }
}