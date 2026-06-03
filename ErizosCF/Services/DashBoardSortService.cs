using CommunityToolkit.Mvvm.ComponentModel;

public partial class DashboardSortService : ObservableObject
{
    public DashboardSortService() { }

    [ObservableProperty]
    private SortField currentSortField = SortField.Usuario;

    [ObservableProperty]
    private SortDirection currentSortDirection = SortDirection.Asc;

    public event Action? OrdenamientoCambiado;

    public enum SortField
    {
        Usuario,
        Nombre,
        Curso,
        Escuela,
        Rating,
        Team,
        Individual,
        Unrated
    }

    public enum SortDirection
    {
        Asc,
        Desc
    }

    partial void OnCurrentSortFieldChanged(SortField value)
    {
        OrdenamientoCambiado?.Invoke();
    }

    partial void OnCurrentSortDirectionChanged(SortDirection value)
    {
        OrdenamientoCambiado?.Invoke();
    }

    public void Reset()
    {
        CurrentSortField = SortField.Usuario;
        CurrentSortDirection = SortDirection.Asc;
    }
}