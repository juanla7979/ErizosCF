using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErizosCF.Models;
using ErizosCF.Services;
using Microcharts;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ErizosCF.ViewModels
{
    public partial class CompareTeamsViewModel : ObservableObject
    {
        private readonly CFService _cfService = new();

        [ObservableProperty] private int totalProblemas1;
        [ObservableProperty] private int totalProblemas2;

        // Equipo 1 - 3 miembros
        [ObservableProperty] private string team1Member1;
        [ObservableProperty] private string team1Member2;
        [ObservableProperty] private string team1Member3;

        // Equipo 2 - 3 miembros
        [ObservableProperty] private string team2Member1;
        [ObservableProperty] private string team2Member2;
        [ObservableProperty] private string team2Member3;

        [ObservableProperty] private TeamProfile team1;
        [ObservableProperty] private TeamProfile team2;

        [ObservableProperty] private UserAnalytics analytics1;
        [ObservableProperty] private UserAnalytics analytics2;

        [ObservableProperty] private DateTime fechaInicio = DateTime.Now.AddMonths(-1);
        [ObservableProperty] private DateTime fechaFin = DateTime.Now;

        [ObservableProperty] private bool isLoading;

        // GRÁFICAS
        [ObservableProperty] private Chart ratingChart1;
        [ObservableProperty] private Chart ratingChart2;
        [ObservableProperty] private Chart tagChart1;
        [ObservableProperty] private Chart tagChart2;
        [ObservableProperty] private Chart difficultyChart1;
        [ObservableProperty] private Chart difficultyChart2;

        [RelayCommand]
        public async Task Comparar()
        {
            // Obtener miembros del equipo 1
            var miembros1 = new List<string>();
            if (!string.IsNullOrWhiteSpace(Team1Member1)) miembros1.Add(Team1Member1.Trim());
            if (!string.IsNullOrWhiteSpace(Team1Member2)) miembros1.Add(Team1Member2.Trim());
            if (!string.IsNullOrWhiteSpace(Team1Member3)) miembros1.Add(Team1Member3.Trim());

            // Obtener miembros del equipo 2
            var miembros2 = new List<string>();
            if (!string.IsNullOrWhiteSpace(Team2Member1)) miembros2.Add(Team2Member1.Trim());
            if (!string.IsNullOrWhiteSpace(Team2Member2)) miembros2.Add(Team2Member2.Trim());
            if (!string.IsNullOrWhiteSpace(Team2Member3)) miembros2.Add(Team2Member3.Trim());

            // Validar que cada equipo tenga al menos un miembro
            if (miembros1.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "El Equipo 1 debe tener al menos un miembro.", "OK");
                return;
            }

            if (miembros2.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "El Equipo 2 debe tener al menos un miembro.", "OK");
                return;
            }

            // Validar que las fechas sean correctas
            if (FechaInicio > FechaFin)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "La fecha de inicio no puede ser mayor a la fecha fin.", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                // VALIDAR QUE TODOS LOS USUARIOS EXISTAN EN CODEFORCES
                var usuariosInvalidos1 = new List<string>();
                var usuariosInvalidos2 = new List<string>();

                // Validar miembros del equipo 1
                foreach (var handle in miembros1)
                {
                    var user = await _cfService.GetUserInfoAsync(handle);
                    if (user == null)
                    {
                        usuariosInvalidos1.Add(handle);
                    }
                }

                // Validar miembros del equipo 2
                foreach (var handle in miembros2)
                {
                    var user = await _cfService.GetUserInfoAsync(handle);
                    if (user == null)
                    {
                        usuariosInvalidos2.Add(handle);
                    }
                }

                // Mostrar errores si hay usuarios inválidos
                if (usuariosInvalidos1.Any())
                {
                    var mensaje = $"Los siguientes usuarios del Equipo 1 no existen en Codeforces:\n\n{string.Join("\n", usuariosInvalidos1)}";
                    await Application.Current.MainPage.DisplayAlert("Usuarios No Encontrados", mensaje, "OK");
                    return;
                }

                if (usuariosInvalidos2.Any())
                {
                    var mensaje = $"Los siguientes usuarios del Equipo 2 no existen en Codeforces:\n\n{string.Join("\n", usuariosInvalidos2)}";
                    await Application.Current.MainPage.DisplayAlert("Usuarios No Encontrados", mensaje, "OK");
                    return;
                }

                // Crear nombres mostrando los miembros
                string nombreEquipo1 = string.Join(", ", miembros1);
                string nombreEquipo2 = string.Join(", ", miembros2);

                Team1 = new TeamProfile { Name = nombreEquipo1, Members = miembros1 };
                Team2 = new TeamProfile { Name = nombreEquipo2, Members = miembros2 };

                Debug.WriteLine($"Equipo 1 - Miembros: {string.Join(", ", Team1.Members)}");
                Debug.WriteLine($"Equipo 2 - Miembros: {string.Join(", ", Team2.Members)}");

                // Calcular rating promedio
                Team1.AvgRating = await CalcularRatingPromedio(miembros1);
                Team2.AvgRating = await CalcularRatingPromedio(miembros2);

                // Cargar datos en paralelo
                var task1 = CargarDatosEquipo(miembros1);
                var task2 = CargarDatosEquipo(miembros2);

                await Task.WhenAll(task1, task2);

                Analytics1 = await task1;
                Analytics2 = await task2;

                TotalProblemas1 = Analytics1.TagStats.Sum(t => t.Count);
                TotalProblemas2 = Analytics2.TagStats.Sum(t => t.Count);

                // Notificar cambios a la UI
                OnPropertyChanged(nameof(Team1));
                OnPropertyChanged(nameof(Team2));
                OnPropertyChanged(nameof(Analytics1));
                OnPropertyChanged(nameof(Analytics2));
                OnPropertyChanged(nameof(TotalProblemas1));
                OnPropertyChanged(nameof(TotalProblemas2));

                GenerarGraficas();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en Comparar: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Ocurrió un error al comparar los equipos:\n{ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<int> CalcularRatingPromedio(List<string> miembros)
        {
            var ratings = new List<int>();
            foreach (var handle in miembros)
            {
                try
                {
                    var user = await _cfService.GetUserInfoAsync(handle);
                    if (user != null && user.CurrentRating > 0)
                    {
                        ratings.Add(user.CurrentRating);
                        Debug.WriteLine($"Rating de {handle}: {user.CurrentRating}");
                    }
                    else
                    {
                        Debug.WriteLine($"Usuario {handle} no encontrado o sin rating");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al obtener rating de {handle}: {ex.Message}");
                }
            }
            return ratings.Any() ? (int)ratings.Average() : 0;
        }

        private async Task<UserAnalytics> CargarDatosEquipo(List<string> miembros)
        {
            var analisisTotal = new UserAnalytics();
            var todosLosProblemas = new List<ProblemStats>();
            var ratingHistoryTotal = new List<RatingHistory>();
            var problemasUnicos = new HashSet<string>();

            foreach (var handle in miembros)
            {
                try
                {
                    Debug.WriteLine($"Cargando datos para: {handle}");

                    var problemas = await _cfService.GetUserStatusAsync(handle, FechaInicio, FechaFin);
                    var ratingHistory = await _cfService.GetRatingHistoryAsync(handle, FechaInicio, FechaFin);

                    foreach (var problema in problemas)
                    {
                        if (problemasUnicos.Add(problema.ProblemName))
                        {
                            todosLosProblemas.Add(problema);
                        }
                    }

                    ratingHistoryTotal.AddRange(ratingHistory);

                    Debug.WriteLine($"  Problemas cargados: {problemas.Count}");
                    Debug.WriteLine($"  Rating history: {ratingHistory.Count}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error cargando datos para {handle}: {ex.Message}");
                }
            }

            // Tags
            var tagStats = _cfService.GetTagStats(todosLosProblemas);
            foreach (var tag in tagStats.OrderByDescending(x => x.Value).Take(15))
            {
                analisisTotal.TagStats.Add(new TagStats { Tag = tag.Key, Count = tag.Value });
            }

            // Dificultad
            var difficultyStats = _cfService.GetDifficultyStats(todosLosProblemas);
            foreach (var diff in difficultyStats.OrderBy(x => x.Key))
            {
                analisisTotal.DifficultyStats.Add(new DifficultyStats
                {
                    Difficulty = diff.Key,
                    Count = diff.Value,
                    Percentage = todosLosProblemas.Count > 0 ? (double)diff.Value / todosLosProblemas.Count : 0
                });
            }

            // Rating History (promedio por fecha)
            if (ratingHistoryTotal.Any())
            {
                var ratingPorFecha = ratingHistoryTotal
                    .GroupBy(r => r.ContestDate.Date)
                    .Select(g => new RatingHistory
                    {
                        ContestName = g.First().ContestName,
                        ContestDate = g.Key,
                        NewRating = (int)Math.Round(g.Average(r => r.NewRating)),
                        Rank = (int)Math.Round(g.Average(r => r.Rank))
                    })
                    .OrderBy(r => r.ContestDate)
                    .ToList();

                foreach (var rating in ratingPorFecha)
                {
                    analisisTotal.RatingHistory.Add(rating);
                }
            }

            Debug.WriteLine($"Total problemas únicos: {todosLosProblemas.Count}");
            Debug.WriteLine($"Tags únicos: {analisisTotal.TagStats.Count}");
            Debug.WriteLine($"Rating history puntos: {analisisTotal.RatingHistory.Count}");

            return analisisTotal;
        }

        private void GenerarGraficas()
        {
            try
            {
                // Rating Chart - Equipo 1
                if (Analytics1?.RatingHistory.Any() == true)
                {
                    var entries = Analytics1.RatingHistory.Select((r, i) => new ChartEntry(r.NewRating)
                    {
                        Label = (i + 1).ToString(),
                        ValueLabel = r.NewRating.ToString(),
                        Color = SKColor.Parse("#9B59B6"),
                        ValueLabelColor = SKColor.Parse("#FFFFFF")
                    }).ToList();

                    RatingChart1 = new LineChart
                    {
                        Entries = entries,
                        LabelTextSize = 24,
                        IsAnimated = true,
                        Margin = 15,
                        PointSize = 8,
                        LineSize = 3,
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // Rating Chart - Equipo 2
                if (Analytics2?.RatingHistory.Any() == true)
                {
                    var entries = Analytics2.RatingHistory.Select((r, i) => new ChartEntry(r.NewRating)
                    {
                        Label = (i + 1).ToString(),
                        ValueLabel = r.NewRating.ToString(),
                        Color = SKColor.Parse("#FF6B6B"),
                        ValueLabelColor = SKColor.Parse("#FFFFFF")
                    }).ToList();

                    RatingChart2 = new LineChart
                    {
                        Entries = entries,
                        LabelTextSize = 24,
                        IsAnimated = true,
                        Margin = 15,
                        PointSize = 8,
                        LineSize = 3,
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // Tag Chart - Equipo 1
                if (Analytics1?.TagStats.Any() == true)
                {
                    var entries = Analytics1.TagStats
                        .OrderByDescending(t => t.Count)
                        .Take(15)
                        .Select(tag => new ChartEntry(tag.Count)
                        {
                            Label = tag.Tag.Length > 15 ? tag.Tag.Substring(0, 12) + ".." : tag.Tag,
                            ValueLabel = tag.Count.ToString(),
                            Color = TagColorService.GetColorForTag(tag.Tag),
                            ValueLabelColor = SKColor.Parse("#FFFFFF"),
                            TextColor = SKColor.Parse("#FFFFFF")
                        }).ToList();

                    TagChart1 = new PieChart
                    {
                        Entries = entries,
                        IsAnimated = true,
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelTextSize = 20,
                        Margin = 10,
                        HoleRadius = 0.35f,
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // Tag Chart - Equipo 2
                if (Analytics2?.TagStats.Any() == true)
                {
                    var entries = Analytics2.TagStats
                        .OrderByDescending(t => t.Count)
                        .Take(15)
                        .Select(tag => new ChartEntry(tag.Count)
                        {
                            Label = tag.Tag.Length > 15 ? tag.Tag.Substring(0, 12) + ".." : tag.Tag,
                            ValueLabel = tag.Count.ToString(),
                            Color = TagColorService.GetColorForTag(tag.Tag),
                            ValueLabelColor = SKColor.Parse("#FFFFFF"),
                            TextColor = SKColor.Parse("#FFFFFF")
                        }).ToList();

                    TagChart2 = new PieChart
                    {
                        Entries = entries,
                        IsAnimated = true,
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelTextSize = 20,
                        Margin = 10,
                        HoleRadius = 0.35f,
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // Difficulty Charts - Equipo 1
                if (Analytics1?.DifficultyStats.Any() == true)
                {
                    var entries = Analytics1.DifficultyStats
                        .Where(d => d.Count > 0)
                        .Select(d => new ChartEntry(d.Count)
                        {
                            Label = d.Difficulty.ToString(),
                            ValueLabel = d.Count.ToString(),
                            Color = SKColor.Parse("#9B59B6"),
                            ValueLabelColor = SKColor.Parse("#FFFFFF")
                        }).ToList();

                    DifficultyChart1 = new BarChart
                    {
                        Entries = entries,
                        IsAnimated = true,
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelTextSize = 20,
                        Margin = 10,
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // Difficulty Charts - Equipo 2
                if (Analytics2?.DifficultyStats.Any() == true)
                {
                    var entries = Analytics2.DifficultyStats
                        .Where(d => d.Count > 0)
                        .Select(d => new ChartEntry(d.Count)
                        {
                            Label = d.Difficulty.ToString(),
                            ValueLabel = d.Count.ToString(),
                            Color = SKColor.Parse("#FF6B6B"),
                            ValueLabelColor = SKColor.Parse("#FFFFFF")
                        }).ToList();

                    DifficultyChart2 = new BarChart
                    {
                        Entries = entries,
                        IsAnimated = true,
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelTextSize = 20,
                        Margin = 10,
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                Debug.WriteLine("Gráficas generadas exitosamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generando gráficas: {ex.Message}");
            }
        }

        // Método para limpiar los campos
        [RelayCommand]
        public void LimpiarCampos()
        {
            Team1Member1 = string.Empty;
            Team1Member2 = string.Empty;
            Team1Member3 = string.Empty;
            Team2Member1 = string.Empty;
            Team2Member2 = string.Empty;
            Team2Member3 = string.Empty;

            Team1 = null;
            Team2 = null;
            Analytics1 = null;
            Analytics2 = null;
            TotalProblemas1 = 0;
            TotalProblemas2 = 0;

            RatingChart1 = null;
            RatingChart2 = null;
            TagChart1 = null;
            TagChart2 = null;
            DifficultyChart1 = null;
            DifficultyChart2 = null;
        }
    }
}