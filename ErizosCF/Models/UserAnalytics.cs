using System.Collections.ObjectModel;

namespace ErizosCF.Models
{
    public class UserAnalytics
    {
        public ObservableCollection<RatingHistory> RatingHistory { get; set; }
        public ObservableCollection<DifficultyStats> DifficultyStats { get; set; }
        public ObservableCollection<TagStats> TagStats { get; set; }

        public UserAnalytics()
        {
            RatingHistory = new ObservableCollection<RatingHistory>();
            DifficultyStats = new ObservableCollection<DifficultyStats>();
            TagStats = new ObservableCollection<TagStats>();
        }
    }
}