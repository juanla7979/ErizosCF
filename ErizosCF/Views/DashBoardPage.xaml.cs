using ErizosCF.ViewModels;
using ErizosCF.Models;
namespace ErizosCF.Views;

public partial class DashBoardPage : ContentPage
{
    // primer tabla
    private double _lastContentScrollX1;
    private double _lastHeaderScrollX1;
    private bool _isSyncing1;

    // segunda tabla
    private double _lastContentScrollX2;
    private double _lastHeaderScrollX2;
    private bool _isSyncing2;

    public DashBoardPage()
    {
        InitializeComponent();

        // Solo crear el servicio de filtros (como ya tienes)
        var filterService = new DashboardFilterService();
        var sortService = new DashboardSortService(); // Agrega esta línea

        // Pasar ambos servicios (modifica esta línea)
        BindingContext = new DashBoardViewModel(filterService, sortService); // Agrega sortService
    }

    // primer tabla
    private void OnContentHorizontalScroll1(object sender, ScrolledEventArgs e)
    {
        if (!((DashBoardViewModel)BindingContext).EncabezadosHabilitados) return;

        if (_isSyncing1) return;
        _isSyncing1 = true;
        _lastContentScrollX1 = e.ScrollX;
        HeaderScrollView1.ScrollToAsync(e.ScrollX, 0, false);
        _isSyncing1 = false;
    }

    private void OnHeaderScrollViewScrolled1(object sender, ScrolledEventArgs e)
    {
        if (!((DashBoardViewModel)BindingContext).EncabezadosHabilitados) return;

        if (_isSyncing1) return;
        _isSyncing1 = true;
        _lastHeaderScrollX1 = e.ScrollX;
        ContentScrollView1.ScrollToAsync(e.ScrollX, 0, false);
        _isSyncing1 = false;
    }

    // segunda tabla

    private void OnContentHorizontalScroll2(object sender, ScrolledEventArgs e)
    {
        if (!((DashBoardViewModel)BindingContext).EncabezadosHabilitados) return;

        if (_isSyncing1) return;
        _isSyncing2 = true;
        _lastContentScrollX2 = e.ScrollX;
        HeaderScrollView2.ScrollToAsync(e.ScrollX, 0, false);
        _isSyncing2 = false;
    }

    private void OnHeaderScrollViewScrolled2(object sender, ScrolledEventArgs e)
    {
        if (!((DashBoardViewModel)BindingContext).EncabezadosHabilitados) return;

        if (_isSyncing2) return;
        _isSyncing2 = true;
        _lastHeaderScrollX2 = e.ScrollX;
        ContentScrollView2.ScrollToAsync(e.ScrollX, 0, false);
        _isSyncing2 = false;
    }
}