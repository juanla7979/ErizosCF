using ClosedXML.Excel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErizosCF.Models;
using ErizosCF.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ErizosCF.ViewModels
{
    public partial class DashBoardViewModel : ObservableObject
    {
        private readonly CFService _cfService;
        public DashboardFilterService Filtros { get; }
        public DashboardSortService Ordenador { get; }

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<UserProfile> _todosUsuariosResumen = new();

        [ObservableProperty]
        private ObservableCollection<UserProfile> _usuariosResumen = new();

        [ObservableProperty]
        private DateTime _fechaInicio = DateTime.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTime _fechaFin = DateTime.Now;

        [ObservableProperty]
        private List<string> _encabezadosSemanas = new();

        [ObservableProperty]
        private bool _datosCargados;

        [ObservableProperty]
        private bool _isFiltrable;

        // Agrega estas propiedades en tu DashBoardViewModel (después de SortAscending)

        [ObservableProperty]
        private bool _sortDescending = false;

        // Modifica SortAscending para que actualice SortDescending también
        partial void OnSortAscendingChanged(bool value)
        {
            if (value)
            {
                SortDescending = false;
            }
            OrdenarUsuarios();
        }

        partial void OnSortDescendingChanged(bool value)
        {
            if (value)
            {
                SortAscending = false;
            }
            OrdenarUsuarios();
        }

        // Agrega esto al final de tu DashBoardViewModel, antes de la última llave

        [ObservableProperty]
        private string _sortBy = "Usuario"; // Usuario, Nombre, Curso, Escuela, Rating, Team, Individual, Unrated

        [ObservableProperty]
        private bool _sortAscending = true;

        public Array SortOptions => new[] { "Usuario", "Nombre", "Curso", "Escuela", "Rating", "Team", "Individual", "Unrated" };

        public bool EncabezadosHabilitados => DatosCargados;

        // Propiedades para el binding del ordenamiento
        public Array SortFields => Enum.GetValues(typeof(DashboardSortService.SortField));
        public Array SortDirections => Enum.GetValues(typeof(DashboardSortService.SortDirection));

        public DashBoardViewModel(DashboardFilterService filtros, DashboardSortService ordenador)
        {
            _cfService = new CFService();
            Filtros = filtros;
            Ordenador = ordenador;

            // Suscribirse a eventos de cambios
            Filtros.FiltrosCambiaron += OnFiltrosChanged;
            Ordenador.OrdenamientoCambiado += OnOrdenamientoChanged;
        }

        private async void OnFiltrosChanged()
        {
            if (DatosCargados && !IsLoading)
            {
                await AplicarFiltrosYOrdenamiento();
            }
        }

        private async void OnOrdenamientoChanged()
        {
            if (DatosCargados && !IsLoading && UsuariosResumen != null && UsuariosResumen.Any())
            {
                await OrdenarUsuariosActuales();
            }
        }

        [RelayCommand]
        private async Task CargarResumenUsuarios()
        {
            try
            {
                IsLoading = true;
                IsFiltrable = false;
                DatosCargados = false;

                Filtros.Reset();
                Ordenador.Reset();

                if (FechaInicio > FechaFin)
                {
                    await Shell.Current.DisplayAlert("Error", "La fecha de inicio no puede ser mayor a la fecha fin", "OK");
                    return;
                }

                var alumnosDB = await UserProfile.ObtenerTodosUsuariosAsync();
                var usuariosCargados = new List<UserProfile>();

                foreach (var alumno in alumnosDB)
                {
                    var user = await _cfService.GetUserInfoAsync(alumno.Handle);
                    if (user == null) continue;

                    alumno.Problemas = await _cfService.GetUserStatusAsync(alumno.Handle, FechaInicio, FechaFin);
                    await alumno.ActualizarDatosCodeforces(user, alumno.Problemas, alumno.Tipo);
                    alumno.ProblemasPorSemana = new ObservableCollection<int>(
                        ProblemStats.ProblemasSemanales(alumno.Problemas, FechaInicio, FechaFin)
                    );

                    usuariosCargados.Add(alumno);
                }

                TodosUsuariosResumen = new ObservableCollection<UserProfile>(usuariosCargados);
                DatosCargados = true;

                await AplicarFiltrosYOrdenamiento();

                IsFiltrable = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
                DatosCargados = false;
                IsFiltrable = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AplicarFiltrosYOrdenamiento()
        {
            if (TodosUsuariosResumen == null || !TodosUsuariosResumen.Any())
                return;

            try
            {
                IsLoading = true;

                // Aplicar filtros
                var filtrados = await Task.Run(() => FiltrarUsuarios());

                // Actualizar datos según fechas
                await ActualizarDatosUsuarios(filtrados);

                // Aplicar ordenamiento
                var ordenados = AplicarOrdenamiento(filtrados);

                UsuariosResumen = new ObservableCollection<UserProfile>(ordenados);
                CalcularEncabezadosSemanales();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error en filtrado/ordenamiento: {e}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OrdenarUsuariosActuales()
        {
            if (UsuariosResumen == null || !UsuariosResumen.Any())
                return;

            try
            {
                var ordenados = AplicarOrdenamiento(UsuariosResumen.ToList());
                UsuariosResumen = new ObservableCollection<UserProfile>(ordenados);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error en ordenamiento: {e}");
            }
        }

        private List<UserProfile> FiltrarUsuarios()
        {
            var cursosSeleccionados = new List<int>();
            if (Filtros.Curso1Seleccionado) cursosSeleccionados.Add(1);
            if (Filtros.Curso2Seleccionado) cursosSeleccionados.Add(2);
            if (Filtros.Curso3Seleccionado) cursosSeleccionados.Add(3);

            var sexosSeleccionados = new List<string>();
            if (Filtros.Hombres) sexosSeleccionados.Add("M");
            if (Filtros.Mujeres) sexosSeleccionados.Add("F");

            var rangosSeleccionados = new List<int>();
            if (Filtros.Newbie) rangosSeleccionados.Add(0);
            if (Filtros.Pupil) rangosSeleccionados.Add(1);
            if (Filtros.Specialist) rangosSeleccionados.Add(2);
            if (Filtros.Expert) rangosSeleccionados.Add(3);
            if (Filtros.CandidateMaster) rangosSeleccionados.Add(4);
            if (Filtros.Master) rangosSeleccionados.Add(5);
            if (Filtros.InternationalMaster) rangosSeleccionados.Add(6);
            if (Filtros.GrandMaster) rangosSeleccionados.Add(7);
            if (Filtros.InternationalGrandMaster) rangosSeleccionados.Add(8);
            if (Filtros.LegendaryGrandMaster) rangosSeleccionados.Add(9);

            var estadoSeleccionado = new List<string>();
            if (Filtros.Icpc) estadoSeleccionado.Add("ICPC");
            if (Filtros.Excelente) estadoSeleccionado.Add("EXCELENTE");
            if (Filtros.Normal) estadoSeleccionado.Add("NORMAL");
            if (Filtros.Riesgo) estadoSeleccionado.Add("RIESGO");

            return TodosUsuariosResumen
                .Where(u =>
                    cursosSeleccionados.Contains(u.Curso) &&
                    sexosSeleccionados.Contains(u.Sexo) &&
                    rangosSeleccionados.Contains(UserProfile.ObtenerRangoDesdeRating(u.CurrentRating)) &&
                    estadoSeleccionado.Contains(u.Estado) &&
                    ((Filtros.Internos && u.Tipo == "ITSUR") || (Filtros.Externos && u.Tipo == "EXTERNO"))
                )
                .ToList();
        }

        private async Task ActualizarDatosUsuarios(List<UserProfile> usuarios)
        {
            foreach (var u in usuarios)
            {
                var problemas = u.Problemas
                    .Where(p => p.SolvedDate >= FechaInicio && p.SolvedDate <= FechaFin)
                    .ToList();

                await u.ActualizarDatosCodeforces(u, problemas, u.Tipo);
                u.ProblemasPorSemana = new ObservableCollection<int>(
                    ProblemStats.ProblemasSemanales(problemas, FechaInicio, FechaFin)
                );
            }
        }

        private List<UserProfile> AplicarOrdenamiento(List<UserProfile> usuarios)
        {
            return (Ordenador.CurrentSortField, Ordenador.CurrentSortDirection) switch
            {
                (DashboardSortService.SortField.Usuario, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.Handle).ToList(),

                (DashboardSortService.SortField.Usuario, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.Handle).ToList(),

                (DashboardSortService.SortField.Nombre, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.FullName).ToList(),

                (DashboardSortService.SortField.Nombre, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.FullName).ToList(),

                (DashboardSortService.SortField.Curso, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.Curso).ToList(),

                (DashboardSortService.SortField.Curso, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.Curso).ToList(),

                (DashboardSortService.SortField.Escuela, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.NombreEscuela).ToList(),

                (DashboardSortService.SortField.Escuela, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.NombreEscuela).ToList(),

                (DashboardSortService.SortField.Rating, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.CurrentRating).ToList(),

                (DashboardSortService.SortField.Rating, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.CurrentRating).ToList(),

                (DashboardSortService.SortField.Individual, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.Individual).ToList(),

                (DashboardSortService.SortField.Individual, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.Individual).ToList(),

                (DashboardSortService.SortField.Team, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.Team).ToList(),

                (DashboardSortService.SortField.Team, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.Team).ToList(),

                (DashboardSortService.SortField.Unrated, DashboardSortService.SortDirection.Asc)
                    => usuarios.OrderBy(u => u.ProblemasPorDificultad.GetValueOrDefault(-1)).ToList(),

                (DashboardSortService.SortField.Unrated, DashboardSortService.SortDirection.Desc)
                    => usuarios.OrderByDescending(u => u.ProblemasPorDificultad.GetValueOrDefault(-1)).ToList(),

                _ => usuarios.ToList()
            };
        }

        private void CalcularEncabezadosSemanales()
        {
            try
            {
                var diferencia = (FechaFin - FechaInicio).Days + 1;
                var semanas = (int)Math.Ceiling(diferencia / 7.0);

                EncabezadosSemanas = Enumerable.Range(0, semanas)
                    .Select(i => {
                        var inicioSemana = FechaInicio.AddDays(i * 7);
                        var finSemana = inicioSemana.AddDays(6) > FechaFin ? FechaFin : inicioSemana.AddDays(6);
                        return $"{inicioSemana:dd/MM} - {finSemana:dd/MM}";
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error calculando encabezados: {e}");
                EncabezadosSemanas = new List<string>();
            }
        }

        async partial void OnFechaInicioChanged(DateTime value)
        {
            if (FechaInicio <= FechaFin && DatosCargados && !IsLoading)
            {
                await AplicarFiltrosYOrdenamiento();
            }
        }

        async partial void OnFechaFinChanged(DateTime value)
        {
            if (FechaInicio <= FechaFin && DatosCargados && !IsLoading)
            {
                await AplicarFiltrosYOrdenamiento();
            }
        }

        // Métodos de exportación (mantener igual)
        [RelayCommand]
        private async Task ExportarCsvAsync()
        {
            if (UsuariosResumen == null || UsuariosResumen.Count == 0)
            {
                await Shell.Current.DisplayAlert("Sin datos", "No hay usuarios para exportar.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Usuario,Nombre,Curso,Escuela,Rating,Solved,Team,Individual,Unrated");

            var dificultades = new List<int> { 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2300, 2400, 2500 };
            foreach (var dif in dificultades)
                sb.Append($",{dif}");

            foreach (var semana in EncabezadosSemanas)
                sb.Append($",\"{semana}\"");

            sb.AppendLine();

            foreach (var user in UsuariosResumen)
            {
                sb.Append($"{user.Handle},{user.FullName},{user.Curso},{user.NombreEscuela},{user.CurrentRating},{user.TotalSolved},{user.Team},{user.Individual}");
                sb.Append($",{user.ProblemasPorDificultad.GetValueOrDefault(-1)}");

                foreach (var dif in dificultades)
                    sb.Append($",{user.ProblemasPorDificultad.GetValueOrDefault(dif)}");

                foreach (var count in user.ProblemasPorSemana)
                    sb.Append($",{count}");

                sb.AppendLine();
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var stream = new MemoryStream(bytes);
            var result = await FileSaver.Default.SaveAsync("AvanceErizos.csv", stream, default);

            if (result.IsSuccessful)
                await Shell.Current.DisplayAlert("Guardado Correctamente", $"CSV guardado en:\n{result.FilePath}", "OK");
            else
                await Shell.Current.DisplayAlert("Error", "No se pudo guardar el archivo.", "OK");
        }

        [RelayCommand]
        private async Task ExportarExcel()
        {
            try
            {
                var workbook = new XLWorkbook();

                var hojaResumen = workbook.Worksheets.Add("Problemas Por Rating");
                hojaResumen.Cell(1, 1).Value = "Usuario";
                hojaResumen.Cell(1, 2).Value = "Nombre";
                hojaResumen.Cell(1, 3).Value = "Curso";
                hojaResumen.Cell(1, 4).Value = "Escuela";
                hojaResumen.Cell(1, 5).Value = "Rating";
                hojaResumen.Cell(1, 6).Value = "Solved";
                hojaResumen.Cell(1, 7).Value = "Team";
                hojaResumen.Cell(1, 8).Value = "Individual";
                hojaResumen.Cell(1, 9).Value = "Unrated";

                int col = 10;
                var dificultades = new List<int> { 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2300, 2400, 2500 };
                foreach (var d in dificultades)
                    hojaResumen.Cell(1, col++).Value = d.ToString();

                int fila = 2;
                foreach (var user in UsuariosResumen)
                {
                    col = 1;
                    hojaResumen.Cell(fila, col++).Value = user.Handle;
                    hojaResumen.Cell(fila, col++).Value = user.FullName;
                    hojaResumen.Cell(fila, col++).Value = user.Curso;
                    hojaResumen.Cell(fila, col++).Value = user.NombreEscuela;
                    hojaResumen.Cell(fila, col++).Value = user.CurrentRating;
                    hojaResumen.Cell(fila, col++).Value = user.TotalSolved;
                    hojaResumen.Cell(fila, col++).Value = user.Team;
                    hojaResumen.Cell(fila, col++).Value = user.Individual;
                    hojaResumen.Cell(fila, col++).Value = user.ProblemasPorDificultad.GetValueOrDefault(-1);

                    foreach (var d in dificultades)
                        hojaResumen.Cell(fila, col++).Value = user.ProblemasPorDificultad.GetValueOrDefault(d);

                    fila++;
                }

                hojaResumen.Columns().AdjustToContents();

                var hojaSemanas = workbook.Worksheets.Add("Problemas Por Semanas");
                hojaSemanas.Cell(1, 1).Value = "Usuario";
                hojaSemanas.Cell(1, 2).Value = "Nombre";
                hojaSemanas.Cell(1, 3).Value = "Curso";
                hojaSemanas.Cell(1, 4).Value = "Escuela";
                hojaSemanas.Cell(1, 5).Value = "Rating";
                hojaSemanas.Cell(1, 6).Value = "Solved";
                hojaSemanas.Cell(1, 7).Value = "Team";
                hojaSemanas.Cell(1, 8).Value = "Individual";
                hojaSemanas.Cell(1, 9).Value = "Unrated";

                for (int i = 0; i < EncabezadosSemanas.Count; i++)
                    hojaSemanas.Cell(1, i + 10).Value = EncabezadosSemanas[i];

                fila = 2;
                foreach (var user in UsuariosResumen)
                {
                    col = 1;
                    hojaSemanas.Cell(fila, col++).Value = user.Handle;
                    hojaSemanas.Cell(fila, col++).Value = user.FullName;
                    hojaSemanas.Cell(fila, col++).Value = user.Curso;
                    hojaSemanas.Cell(fila, col++).Value = user.NombreEscuela;
                    hojaSemanas.Cell(fila, col++).Value = user.CurrentRating;
                    hojaSemanas.Cell(fila, col++).Value = user.TotalSolved;
                    hojaSemanas.Cell(fila, col++).Value = user.Team;
                    hojaSemanas.Cell(fila, col++).Value = user.Individual;
                    hojaSemanas.Cell(fila, col++).Value = user.ProblemasPorDificultad.GetValueOrDefault(-1);

                    for (int i = 0; i < user.ProblemasPorSemana.Count; i++)
                        hojaSemanas.Cell(fila, i + col).Value = user.ProblemasPorSemana[i];

                    fila++;
                }

                hojaSemanas.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var result = await FileSaver.Default.SaveAsync("AvanceErizos.xlsx", stream, default);

                if (result.IsSuccessful)
                    await Shell.Current.DisplayAlert("Guardado Correctamente", $"Archivo Excel guardado en:\n{result.FilePath}", "OK");
                else
                    await Shell.Current.DisplayAlert("Error", "No se pudo guardar el archivo.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error al exportar: {ex.Message}", "OK");
            }
        }

        // Agrega este método en tu DashBoardViewModel
        private void OrdenarUsuarios()
        {
            if (UsuariosResumen == null || UsuariosResumen.Count == 0) return;

            var usuarios = UsuariosResumen.ToList();

            // Ordenar según la opción seleccionada
            switch (SortBy)
            {
                case "Usuario":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.Handle).ToList() : usuarios.OrderByDescending(u => u.Handle).ToList();
                    break;
                case "Nombre":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.FullName).ToList() : usuarios.OrderByDescending(u => u.FullName).ToList();
                    break;
                case "Curso":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.Curso).ToList() : usuarios.OrderByDescending(u => u.Curso).ToList();
                    break;
                case "Escuela":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.NombreEscuela).ToList() : usuarios.OrderByDescending(u => u.NombreEscuela).ToList();
                    break;
                case "Rating":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.CurrentRating).ToList() : usuarios.OrderByDescending(u => u.CurrentRating).ToList();
                    break;
                case "Team":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.Team).ToList() : usuarios.OrderByDescending(u => u.Team).ToList();
                    break;
                case "Individual":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.Individual).ToList() : usuarios.OrderByDescending(u => u.Individual).ToList();
                    break;
                case "Unrated":
                    usuarios = SortAscending ? usuarios.OrderBy(u => u.ProblemasPorDificultad.GetValueOrDefault(-1)).ToList()
                                             : usuarios.OrderByDescending(u => u.ProblemasPorDificultad.GetValueOrDefault(-1)).ToList();
                    break;
            }

            UsuariosResumen = new ObservableCollection<UserProfile>(usuarios);
        }

        // Eventos para cuando cambian las opciones de ordenamiento
        partial void OnSortByChanged(string value)
        {
            OrdenarUsuarios();
        }

    }
}