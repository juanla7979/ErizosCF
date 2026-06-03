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
    public partial class CompareViewModel : ObservableObject
    {
        private readonly CFService _cfService = new();

        [ObservableProperty] private string handle1;
        [ObservableProperty] private string handle2;

        [ObservableProperty] private UserProfile user1;
        [ObservableProperty] private UserProfile user2;

        [ObservableProperty] private UserAnalytics analytics1;
        [ObservableProperty] private UserAnalytics analytics2;

        [ObservableProperty] private DateTime fechaInicio = DateTime.Now.AddMonths(-1);
        [ObservableProperty] private DateTime fechaFin = DateTime.Now;

        [ObservableProperty] private bool isLoading;

        // GRAFICAS
        [ObservableProperty] private Chart ratingChart1;
        [ObservableProperty] private Chart ratingChart2;
        [ObservableProperty] private Chart tagChart1;
        [ObservableProperty] private Chart tagChart2;
        [ObservableProperty] private Chart difficultyChart1;
        [ObservableProperty] private Chart difficultyChart2;

        [RelayCommand]
        public async Task Comparar()
        {
            // Validar que los handles no estén vacíos
            if (string.IsNullOrWhiteSpace(Handle1))
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Debe ingresar el nombre de usuario del primer competidor.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Handle2))
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Debe ingresar el nombre de usuario del segundo competidor.", "OK");
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

                // Validar que los usuarios existan en Codeforces
                var user1Info = await _cfService.GetUserInfoAsync(Handle1.Trim());
                if (user1Info == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"El usuario '{Handle1}' no existe en Codeforces.", "OK");
                    return;
                }

                var user2Info = await _cfService.GetUserInfoAsync(Handle2.Trim());
                if (user2Info == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"El usuario '{Handle2}' no existe en Codeforces.", "OK");
                    return;
                }

                // Cargar datos completos de ambos usuarios
                var user1Task = CargarUsuarioCompleto(Handle1.Trim());
                var user2Task = CargarUsuarioCompleto(Handle2.Trim());

                await Task.WhenAll(user1Task, user2Task);

                var (u1, a1) = await user1Task;
                var (u2, a2) = await user2Task;

                if (u1 == null || a1 == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"No se pudieron cargar los datos del usuario '{Handle1}'.", "OK");
                    return;
                }

                if (u2 == null || a2 == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"No se pudieron cargar los datos del usuario '{Handle2}'.", "OK");
                    return;
                }

                User1 = u1;
                User2 = u2;
                Analytics1 = a1;
                Analytics2 = a2;

                GenerarGraficas();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en Comparar: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Ocurrió un error al comparar los usuarios:\n{ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<(UserProfile, UserAnalytics)> CargarUsuarioCompleto(string handle)
        {
            try
            {
                var user = await _cfService.GetUserInfoAsync(handle);
                if (user == null) return (null, null);

                var problemas = await _cfService.GetUserStatusAsync(handle, FechaInicio, FechaFin);
                var ratingHistory = await _cfService.GetRatingHistoryAsync(handle, FechaInicio, FechaFin);

                var analytics = new UserAnalytics();

                // TAGS
                var tagStats = _cfService.GetTagStats(problemas);
                foreach (var tag in tagStats.OrderByDescending(x => x.Value).Take(10))
                {
                    analytics.TagStats.Add(new TagStats
                    {
                        Tag = tag.Key,
                        Count = tag.Value
                    });
                }

                // DIFICULTAD
                var difficultyStats = _cfService.GetDifficultyStats(problemas);
                foreach (var diff in difficultyStats.OrderBy(x => x.Key))
                {
                    analytics.DifficultyStats.Add(new DifficultyStats
                    {
                        Difficulty = diff.Key,
                        Count = diff.Value,
                        Percentage = problemas.Count > 0 ? (double)diff.Value / problemas.Count : 0
                    });
                }

                // RATING HISTORY
                foreach (var r in ratingHistory)
                    analytics.RatingHistory.Add(r);

                user.TotalSolved = problemas.Count;

                // Calcular promedio diario
                var days = (FechaFin - FechaInicio).Days;
                if (days > 0)
                    user.AvgDailyProblems = (double)problemas.Count / days;
                else
                    user.AvgDailyProblems = 0;

                return (user, analytics);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cargando usuario {handle}: {ex.Message}");
                return (null, null);
            }
        }

        private void GenerarGraficas()
        {
            try
            {
                // Rating Chart - Usuario 1
                if (Analytics1 != null && Analytics1.RatingHistory.Any())
                {
                    var entries = new List<ChartEntry>();

                    for (int i = 0; i < Analytics1.RatingHistory.Count; i++)
                    {
                        entries.Add(new ChartEntry(Analytics1.RatingHistory[i].NewRating)
                        {
                            Label = (i + 1).ToString(),
                            ValueLabel = Analytics1.RatingHistory[i].NewRating.ToString(),
                            Color = SKColor.Parse("#FF6B6B"),
                            ValueLabelColor = SKColor.Parse("#FFFFFF")
                        });
                    }

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

                // Rating Chart - Usuario 2
                if (Analytics2 != null && Analytics2.RatingHistory.Any())
                {
                    var entries = new List<ChartEntry>();

                    for (int i = 0; i < Analytics2.RatingHistory.Count; i++)
                    {
                        entries.Add(new ChartEntry(Analytics2.RatingHistory[i].NewRating)
                        {
                            Label = (i + 1).ToString(),
                            ValueLabel = Analytics2.RatingHistory[i].NewRating.ToString(),
                            Color = SKColor.Parse("#4ECDC4"),
                            ValueLabelColor = SKColor.Parse("#FFFFFF")
                        });
                    }

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

                // TAG CHART - USUARIO 1
                if (Analytics1 != null && Analytics1.TagStats.Any())
                {
                    var entries = new List<ChartEntry>();

                    var topTags = Analytics1.TagStats
                        .OrderByDescending(t => t.Count)
                        .Take(15)
                        .ToList();

                    foreach (var tag in topTags)
                    {
                        var color = TagColorService.GetColorForTag(tag.Tag);

                        entries.Add(new ChartEntry(tag.Count)
                        {
                            Label = tag.Tag.Length > 15 ? tag.Tag.Substring(0, 12) + ".." : tag.Tag,
                            ValueLabel = tag.Count.ToString(),
                            Color = color,
                            ValueLabelColor = SKColor.Parse("#FFFFFF"),
                            TextColor = SKColor.Parse("#FFFFFF")
                        });
                    }

                    TagChart1 = new PieChart
                    {
                        Entries = entries,
                        IsAnimated = true,
                        AnimationDuration = TimeSpan.FromMilliseconds(800),
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelTextSize = 20,
                        Margin = 10,
                        HoleRadius = 0.35f,
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // TAG CHART - USUARIO 2
                if (Analytics2 != null && Analytics2.TagStats.Any())
                {
                    var entries = new List<ChartEntry>();

                    var topTags = Analytics2.TagStats
                        .OrderByDescending(t => t.Count)
                        .Take(15)
                        .ToList();

                    foreach (var tag in topTags)
                    {
                        var color = TagColorService.GetColorForTag(tag.Tag);

                        entries.Add(new ChartEntry(tag.Count)
                        {
                            Label = tag.Tag.Length > 15 ? tag.Tag.Substring(0, 12) + ".." : tag.Tag,
                            ValueLabel = tag.Count.ToString(),
                            Color = color,
                            ValueLabelColor = SKColor.Parse("#FFFFFF"),
                            TextColor = SKColor.Parse("#FFFFFF")
                        });
                    }

                    TagChart2 = new PieChart
                    {
                        Entries = entries,
                        IsAnimated = true,
                        AnimationDuration = TimeSpan.FromMilliseconds(800),
                        BackgroundColor = SKColor.Parse("#2A2A3A"),
                        LabelTextSize = 20,
                        Margin = 10,
                        HoleRadius = 0.35f,
                        LabelColor = SKColor.Parse("#FFFFFF")
                    };
                }

                // DIFFICULTY CHARTS - USUARIO 1 (BARCHART)
                if (Analytics1?.DifficultyStats.Any() == true)
                {
                    var entries = Analytics1.DifficultyStats
                        .Where(d => d.Count > 0)
                        .Select(d => new ChartEntry(d.Count)
                        {
                            Label = d.Difficulty.ToString(),
                            ValueLabel = d.Count.ToString(),
                            Color = SKColor.Parse("#FF6B6B"),
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

                // DIFFICULTY CHARTS - USUARIO 2 (BARCHART)
                if (Analytics2?.DifficultyStats.Any() == true)
                {
                    var entries = Analytics2.DifficultyStats
                        .Where(d => d.Count > 0)
                        .Select(d => new ChartEntry(d.Count)
                        {
                            Label = d.Difficulty.ToString(),
                            ValueLabel = d.Count.ToString(),
                            Color = SKColor.Parse("#4ECDC4"),
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

                Debug.WriteLine("Graficas generadas exitosamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generando graficas: {ex.Message}");
            }
        }

        // Metodo para limpiar los campos
        [RelayCommand]
        public void LimpiarCampos()
        {
            Handle1 = string.Empty;
            Handle2 = string.Empty;

            User1 = null;
            User2 = null;
            Analytics1 = null;
            Analytics2 = null;

            RatingChart1 = null;
            RatingChart2 = null;
            TagChart1 = null;
            TagChart2 = null;
            DifficultyChart1 = null;
            DifficultyChart2 = null;
        }
    }
}