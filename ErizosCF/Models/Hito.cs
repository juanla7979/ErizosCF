using System;

namespace ErizosCF.Models
{
    public class Hito
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaRealizacion { get; set; }
        public string? Lugar { get; set; }
    }
}