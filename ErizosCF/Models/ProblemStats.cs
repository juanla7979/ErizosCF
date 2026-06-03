using System.Diagnostics;

namespace ErizosCF.Models
{
    public class ProblemStats
    {
        public DateTime SolvedDate { get; set; }
        public string ProblemName { get; set; }
        public string ContestId { get; set; }
        public string Index { get; set; }
        public string Verdict { get; set; }
        public int Dificultad { get; set; }
        public int? TeamId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        public static List<int> ProblemasSemanales(List<ProblemStats> problemas, DateTime FechaInicio, DateTime FechaFin)
        {
            int diasDiferencia = (int)Math.Ceiling((FechaFin - FechaInicio).TotalDays / 7.0);
            List<int> semanas = Enumerable.Repeat(0, diasDiferencia).ToList();

            try
            {
                foreach (var p in problemas)
                {
                    if (p.SolvedDate.Date >= FechaInicio.Date && p.SolvedDate.Date <= FechaFin.Date)
                    {
                        int semanaIndex = (int)((p.SolvedDate.Date - FechaInicio.Date).TotalDays / 7.0);
                        semanas[semanaIndex]++;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error al generar los problemas por semana: {e.Message}");
            }

            return semanas;
        }
    }
}
