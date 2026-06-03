namespace ErizosCF.Models
{
    public class TeamProfile
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new();
        public int TotalSolved { get; set; }
        public double AvgDailyProblems { get; set; }
        public int AvgRating { get; set; }
    }
}