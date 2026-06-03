using ErizosCF.Models;
using ErizosCF.ViewModels;

namespace ErizosCF.Views;

public partial class UserManagementPage : ContentPage
{
    public UserManagementPage()
    {
        InitializeComponent();

        BindingContext = new UserManagementViewModel();
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (BindingContext is UserManagementViewModel vm)
        {
            await vm.InicializarAsync();
        }
    }

    private async void OnCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        var checkbox = sender as CheckBox;
        var user = checkbox?.BindingContext as UserProfile;

        var vm = BindingContext as UserManagementViewModel;

        if (vm != null && user != null)
        {
            await vm.CambiarEstadoAsync(user);
        }
    }
}