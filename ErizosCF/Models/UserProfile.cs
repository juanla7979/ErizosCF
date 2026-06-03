using CommunityToolkit.Mvvm.ComponentModel;
using ErizosCF.Services;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace ErizosCF.Models
{
    public partial class UserProfile : ObservableObject
    {
        // API CF
        public string Handle { get; set; }
        public string FirstName { get; set; } = "Sin nombre";
        public string LastName { get; set; } = "Sin apellido";
        public string FullName => $"{FirstName} {LastName}".Trim();
        public int CurrentRating { get; set; }
        public int MaxRating { get; set; }
        public DateTime FechaRegistroCF { get; set; }
        public List<ProblemStats> Problemas { get; set; } = new();

        // BD local
        public int Id { get; set; }
        public string Estado { get; set; } // "ICPC", "Excelente", "Normal", "Riesto".
        public string Sexo { get; set; } // "M" o "F"
        public string Tipo { get; set; }
        public bool Activo { get; set; }
        public string NombreEscuela { get; set; }
        public int Curso { get; set; } // 1, 2, 3

        // Estadisticas calculadas
        [ObservableProperty]
        private Dictionary<int, int> _problemasPorDificultad = new();
        public ObservableCollection<int> TODOSProblemasPorSemana { get; set; } = new();
        public ObservableCollection<int> ProblemasPorSemana { get; set; } = new();
        public int TotalSolved { get; set; }
        public int Individual { get; set; }
        public int Team { get; set; }

        public string Rank { get; set; }
        public double AvgDailyProblems { get; set; }
        public string Efficiency { get; set; }

        // Nuevas propiedades para gráficas
        // Ya existe ProblemasPorDificultad como ObservableProperty, así que no lo duplicamos
        public Dictionary<string, int> ProblemasPorTag { get; set; }

        // Agregar propiedades faltantes que se usan en otros lugares
        public List<RatingChange> RatingHistory { get; set; }
        public List<ProblemStats> ProblemasResueltos { get; set; }

        public UserProfile()
        {
            ProblemasPorSemana = new ObservableCollection<int>();
            RatingHistory = new List<RatingChange>();
            ProblemasResueltos = new List<ProblemStats>();
            ProblemasPorDificultad = new Dictionary<int, int>();
            ProblemasPorTag = new Dictionary<string, int>();
            Efficiency = "0%";
        }

        public UserProfile Clone()
        {
            return new UserProfile
            {
                Handle = Handle,
                FirstName = FirstName,
                LastName = LastName,
                Activo = Activo,
                Estado = Estado,
                Sexo = Sexo,
                Tipo = Tipo,
                Curso = Curso
            };
        }

        // Metodos
        public async Task ActualizarDatosCodeforces(UserProfile user, List<ProblemStats> problemas, string tipo)
        {
            CurrentRating = user.CurrentRating;
            NombreEscuela = tipo;
            TotalSolved = problemas.Count();
            ProblemasPorDificultad.Clear();
            Team = 0;
            Individual = 0;

            if (problemas != null)
            {
                var nuevoDiccionario = new Dictionary<int, int>();
                var tagsDictionary = new Dictionary<string, int>();

                foreach (var p in problemas)
                {
                    // Estadísticas por dificultad
                    int dificultadKey = (p.Dificultad > 0) ? p.Dificultad : -1;
                    nuevoDiccionario[dificultadKey] = nuevoDiccionario.TryGetValue(dificultadKey, out var count) ? count + 1 : 1;

                    // Estadísticas por tags
                    if (p.Tags != null)
                    {
                        foreach (var tag in p.Tags)
                        {
                            tagsDictionary[tag] = tagsDictionary.TryGetValue(tag, out var tagCount) ? tagCount + 1 : 1;
                        }
                    }

                    if (p.TeamId == null) Individual++;
                }

                Team = TotalSolved - Individual;
                ProblemasPorDificultad = nuevoDiccionario;
                ProblemasPorTag = tagsDictionary;
            }
        }

        public static async Task<List<UserProfile>> ObtenerTodosUsuariosAsync()
        {
            var db = new DbService();
            var usuarios = new List<UserProfile>();

            try
            {
                await db.OpenConnectionAsync();

                var query = @"
                            SELECT cuenta_cf, nombre, apellido_paterno, estado, genero, tipo, curso_actual
                            FROM usuarios
                            WHERE cuenta_cf IS NOT NULL
                            and activo = 1
                              AND LOWER(cuenta_cf) NOT IN (
                                '',
                                'no',
                                'none',
                                'ninguna',
                                'ninguno',
                                'null',
                                '0',
                                '.',
                                'x',
                                'no tengo',
                                'xx',
                                'ok',
                                '..',
                                'codeforces'
                              );
                            ";
                using var cmd = new MySqlCommand(query, db.Connection);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var usuario = new UserProfile
                    {
                        Handle = reader.IsDBNull(reader.GetOrdinal("cuenta_cf"))
                        ? ""
                        : reader.GetString("cuenta_cf"),

                        Estado = reader.IsDBNull(reader.GetOrdinal("estado"))
                        ? "Normal"
                        : reader.GetString("estado").ToUpper(),

                        FirstName = reader.IsDBNull(reader.GetOrdinal("nombre"))
                        ? ""
                        : reader.GetString("nombre"),

                        LastName = reader.IsDBNull(reader.GetOrdinal("apellido_paterno"))
                        ? ""
                        : reader.GetString("apellido_paterno"),

                        Sexo = reader.IsDBNull(reader.GetOrdinal("genero"))
                        ? "H"
                        : reader.GetString("genero").ToUpper(),

                        Tipo = reader.IsDBNull(reader.GetOrdinal("tipo"))
                        ? "EXTERNO"
                        : reader.GetString("tipo"),

                        Curso = reader.IsDBNull(reader.GetOrdinal("curso_actual"))
                        ? 1
                        : reader.GetInt32("curso_actual")
                    };

                    usuarios.Add(usuario);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener usuarios: {ex.Message}");
            }
            finally
            {
                await db.CloseConnectionAsync();
            }

            return usuarios;
        }

        public static async Task<string> ObtenerEscuela(int id)
        {
            if (id <= 0) return "Desconocida";

            var db = new DbService();
            string nombreEscuela = "Desconocida";

            try
            {
                await db.OpenConnectionAsync();

                var query = "select nombre from escuelas where id = @id";

                using var cmd = new MySqlCommand(query, db.Connection);
                cmd.Parameters.AddWithValue("@id", id);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null)
                {
                    nombreEscuela = result.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener escuela: {ex.Message}");
            }
            finally
            {
                await db.CloseConnectionAsync();
            }

            return nombreEscuela;
        }

        public async Task GuardarAsync()
        {
            var db = new DbService();
            try
            {
                await db.OpenConnectionAsync();
                // Implementar INSERT/UPDATE en la base de datos
            }
            finally
            {
                await db.CloseConnectionAsync();
            }
        }

        public async Task EliminarAsync()
        {
            var db = new DbService();
            try
            {
                await db.OpenConnectionAsync();
                // Implementar DELETE en la base de datos
            }
            finally
            {
                await db.CloseConnectionAsync();
            }
        }

        public static int ObtenerRangoDesdeRating(int rating)
        {
            if (rating < 1200) return 0;
            if (rating < 1400) return 1;
            if (rating < 1600) return 2;
            if (rating < 1900) return 3;
            if (rating < 2100) return 4;
            if (rating < 2300) return 5;
            if (rating < 2400) return 6;
            if (rating < 2600) return 7;
            if (rating < 3000) return 8;
            return 9;
        }
    }
}