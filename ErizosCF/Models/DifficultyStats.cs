namespace ErizosCF.Models
{
    public class DifficultyStats
    {
        public int Difficulty { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }

        public void CalculatePercentage(int totalSolved)
        {
            if (totalSolved > 0)
                Percentage = (double)Count / totalSolved;
            else
                Percentage = 0;
        }
    }
}