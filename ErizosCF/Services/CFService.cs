using ErizosCF.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace ErizosCF.Services
{
    public class CFService
    {
        private readonly HttpClient _httpClient;

        public CFService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<UserProfile?> GetUserInfoAsync(string handle)
        {
            try
            {
                string url = $"https://codeforces.com/api/user.info?handles={handle}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var user = doc.RootElement.GetProperty("result")[0];

                int? currentRating = null;
                if (user.TryGetProperty("rating", out var r))
                    currentRating = r.GetInt32();

                int? maxRating = null;
                if (user.TryGetProperty("maxRating", out var mr))
                    maxRating = mr.GetInt32();

                return new UserProfile
                {
                    Handle = user.GetProperty("handle").GetString(),
                    FirstName = user.TryGetProperty("firstName", out var fn) ? fn.GetString() : "",
                    LastName = user.TryGetProperty("lastName", out var ln) ? ln.GetString() : "",
                    CurrentRating = (int)currentRating,
                    MaxRating = (int)maxRating
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ContestStat>> GetUserRatingAsync(string handle)
        {
            try
            {
                string url = $"https://codeforces.com/api/user.rating?handle={handle}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return new List<ContestStat>();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var list = new List<ContestStat>();
                foreach (var entry in doc.RootElement.GetProperty("result").EnumerateArray())
                {
                    list.Add(new ContestStat
                    {
                        ContestName = entry.GetProperty("contestName").GetString(),
                        Rank = entry.GetProperty("rank").GetInt32(),
                        NewRating = entry.GetProperty("newRating").GetInt32(),
                        OldRating = entry.GetProperty("oldRating").GetInt32(),
                        ContestDate = DateTimeOffset.FromUnixTimeSeconds(entry.GetProperty("ratingUpdateTimeSeconds").GetInt64()).DateTime
                    });
                }

                return list;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // método para filtrar los problemas de la primera tabla jeje
        public async Task<List<ProblemStats>> GetUserStatusAsync(string handle, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var problemasUnicos = new HashSet<string>();
            var listaFiltrada = new List<ProblemStats>();

            // esto es necesario para que los límites de las fechas, en la fecha final, se cuente el día del límite también
            if (fechaFin.HasValue)
                fechaFin = fechaFin.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            try
            {
                var response = await _httpClient.GetStringAsync($"https://codeforces.com/api/user.status?handle={handle}");
                using JsonDocument doc = JsonDocument.Parse(response);

                // Convertir a lista y ordenar por fecha (más antiguo primero)
                var submissions = doc.RootElement.GetProperty("result").EnumerateArray()
                    .Select(entry => new {
                        Entry = entry,
                        Time = entry.GetProperty("creationTimeSeconds").GetInt64()
                    })
                    .OrderBy(x => x.Time)  // Orden cronológico ascendente
                    .ToList();

                foreach (var sub in submissions)
                {
                    var entry = sub.Entry;

                    // no hay veredicto o es diferente de OK
                    if (!entry.TryGetProperty("verdict", out var verdict) || verdict.GetString() != "OK")
                        continue;

                    // no hay fecha
                    if (!entry.TryGetProperty("creationTimeSeconds", out var time))
                        continue;

                    var fechaResolucion = DateTimeOffset.FromUnixTimeSeconds(time.GetInt64()).ToLocalTime().DateTime;

                    // fecha fuera de rango
                    if (fechaInicio != null && fechaResolucion < fechaInicio.Value) continue;
                    if (fechaFin != null && fechaResolucion > fechaFin.Value) continue;

                    // Obtener el objeto problem
                    var problemElement = entry.GetProperty("problem");

                    // dificultad
                    int dificultad = -1; // no hay rating
                    if (problemElement.TryGetProperty("rating", out var rating))
                    {
                        dificultad = rating.GetInt32();
                        if (dificultad > 2500) continue; // quitar esta línea si se quiere incluir todos los ratings
                    }

                    // 🔥 NUEVO: Extraer los tags del problema
                    var tags = new List<string>();
                    if (problemElement.TryGetProperty("tags", out var tagsElement))
                    {
                        foreach (var tag in tagsElement.EnumerateArray())
                        {
                            tags.Add(tag.GetString());
                        }
                    }

                    // id, no sé como funciona, preguntar al chat y no a mí. Basicamente sirve para añadirlo al hashset
                    // si hay problemas de conteo, muy probablemente se deba a esto. inventarse otra fórmula y revisar si persisten dichos problemas...
                    var problemId = $"{problemElement.GetProperty("contestId")}-{problemElement.GetProperty("index")}";

                    // si no se añade al hash
                    if (!problemasUnicos.Add(problemId))
                        continue;

                    var author = entry.GetProperty("author");

                    bool isTeam = author.TryGetProperty("teamId", out _);
                    int? teamId = isTeam ? author.GetProperty("teamId").GetInt32() : null;

                    // agregar a lista ya filtrada CON LOS TAGS
                    listaFiltrada.Add(new ProblemStats
                    {
                        ProblemName = problemId,
                        Verdict = "OK",
                        SolvedDate = fechaResolucion,
                        Dificultad = dificultad,
                        TeamId = teamId,
                        Tags = tags  // AHORA SÍ se asignan los tags
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener problemas: {ex.Message}");
            }

            return listaFiltrada;
        }

        // NUEVA FUNCIÓN: Obtener estadísticas por dificultad
        public Dictionary<int, int> GetDifficultyStats(List<ProblemStats> problems)
        {
            var stats = new Dictionary<int, int>();

            // Rangos de dificultad estándar de Codeforces
            var difficulties = new List<int> { 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2300, 2400, 2500 };

            foreach (var diff in difficulties)
            {
                stats[diff] = 0;
            }

            foreach (var problem in problems)
            {
                if (problem.Dificultad >= 800 && problem.Dificultad <= 2500)
                {
                    // Agrupar por el rating más cercano (redondeado a la centena más cercana)
                    int roundedDiff = (int)(Math.Round(problem.Dificultad / 100.0) * 100);
                    if (stats.ContainsKey(roundedDiff))
                        stats[roundedDiff]++;
                    else
                        stats[problem.Dificultad] = stats.GetValueOrDefault(problem.Dificultad, 0) + 1;
                }
            }

            return stats;
        }

        // NUEVA FUNCIÓN: Obtener estadísticas por tags
        public Dictionary<string, int> GetTagStats(List<ProblemStats> problems)
        {
            var tagCounts = new Dictionary<string, int>();

            foreach (var problem in problems)
            {
                foreach (var tag in problem.Tags)
                {
                    if (tagCounts.ContainsKey(tag))
                        tagCounts[tag]++;
                    else
                        tagCounts[tag] = 1;
                }
            }

            return tagCounts;
        }

        // NUEVA FUNCIÓN: Obtener historial de rating filtrado por fechas
        public async Task<List<RatingHistory>> GetRatingHistoryAsync(string handle, DateTime fechaInicio, DateTime fechaFin)
        {
            var ratingHistory = new List<RatingHistory>();

            try
            {
                var contests = await GetUserRatingAsync(handle);
                if (contests == null) return ratingHistory;

                foreach (var contest in contests)
                {
                    if (contest.ContestDate >= fechaInicio && contest.ContestDate <= fechaFin)
                    {
                        ratingHistory.Add(new RatingHistory
                        {
                            ContestName = contest.ContestName,
                            NewRating = contest.NewRating,
                            ContestDate = contest.ContestDate,
                            Rank = contest.Rank
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener rating history: {ex.Message}");
            }

            return ratingHistory;
        }
    }
}
