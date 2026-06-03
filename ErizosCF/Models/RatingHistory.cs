namespace ErizosCF.Models
{
    public class RatingHistory
    {
        public string ContestName { get; set; }
        public int NewRating { get; set; }
        public DateTime ContestDate { get; set; }
        public int Rank { get; set; }
    }
}