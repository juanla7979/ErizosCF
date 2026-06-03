using ErizosCF.ViewModels;

namespace ErizosCF.Views
{
    public partial class HitosPage : ContentPage
    {
        public HitosPage()
        {
            InitializeComponent();
            BindingContext = new HitosViewModel();
        }
    }
}