using CommunityToolkit.Mvvm.ComponentModel;
using DocumentFormat.OpenXml.Drawing;
using System.Collections.ObjectModel;
using System.Diagnostics;

public partial class DashboardFilterService : ObservableObject
{
    public DashboardFilterService()
    {
    }

    [ObservableProperty]
    private bool curso1Seleccionado = true;

    [ObservableProperty]
    private bool curso2Seleccionado = true;

    [ObservableProperty]
    private bool curso3Seleccionado = true;

    [ObservableProperty]
    private bool internos = true;

    [ObservableProperty]
    private bool externos = true;

    partial void OnInternosChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    partial void OnExternosChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnCurso1SeleccionadoChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    partial void OnCurso2SeleccionadoChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    partial void OnCurso3SeleccionadoChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    [ObservableProperty]
    private bool hombres = true;

    [ObservableProperty]
    private bool mujeres = true;
    partial void OnHombresChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    partial void OnMujeresChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    [ObservableProperty]
    private bool newbie = true;

    [ObservableProperty]
    private bool pupil = true;

    [ObservableProperty]
    private bool specialist = true;

    [ObservableProperty]
    private bool expert = true;

    [ObservableProperty]
    private bool candidateMaster = true;

    [ObservableProperty]
    private bool master = true;

    [ObservableProperty]
    private bool internationalMaster = true;

    [ObservableProperty]
    private bool grandMaster = true;

    [ObservableProperty]
    private bool internationalGrandMaster = true;

    [ObservableProperty]
    private bool legendaryGrandMaster = true;
    partial void OnNewbieChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnPupilChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnSpecialistChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnExpertChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnCandidateMasterChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnMasterChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnInternationalMasterChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnGrandMasterChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnInternationalGrandMasterChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnLegendaryGrandMasterChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    [ObservableProperty]
    private bool icpc = true;

    [ObservableProperty]
    private bool excelente = true;

    [ObservableProperty]
    private bool normal = true;

    [ObservableProperty]
    private bool riesgo = true;
    partial void OnIcpcChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnExcelenteChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnNormalChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }
    partial void OnRiesgoChanged(bool oldValue, bool newValue)
    {
        FiltrosCambiaron?.Invoke();
    }

    public void Reset()
    {
        Curso1Seleccionado = true;
        Curso2Seleccionado = true;
        Curso3Seleccionado = true;

        Hombres = true;
        Mujeres = true;

        Newbie = true;
        Pupil = true;
        Specialist = true;
        Expert = true;
        CandidateMaster = true;
        Master = true;
        InternationalMaster = true;
        GrandMaster = true;
        InternationalGrandMaster = true;
        LegendaryGrandMaster = true;

        Icpc = true;
        Excelente = true;
        Normal = true;
        Riesgo = true;

        Internos = true;
        Externos = true;
    }

    public event Action FiltrosCambiaron;
}
