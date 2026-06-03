using ErizosCF.ViewModels;

namespace ErizosCF.Views
{
    public partial class CompareTeamPage : ContentPage
    {
        public CompareTeamPage()
        {
            InitializeComponent();
            BindingContext = new CompareTeamsViewModel();
        }
    }
}