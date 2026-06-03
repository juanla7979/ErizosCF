namespace ErizosCF.Models
{
    public class ContestStat
    {
        public int ContestId { get; set; }
        public string ContestName { get; set; }
        public DateTime ContestDate { get; set; }
        public int Rank { get; set; }
        public int OldRating { get; set; }
        public int NewRating { get; set; }
        public int RatingChange => NewRating - OldRating;
    }
}