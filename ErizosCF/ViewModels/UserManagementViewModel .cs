using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using ErizosCF.Models;
using ErizosCF.Services;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;

namespace ErizosCF.ViewModels
{
    public partial class UserManagementViewModel : ObservableObject
    {
        private readonly DbService _db;

        public UserManagementViewModel()
        {
            _db = new DbService();
        }

        private List<UserProfile> snapshot = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string loadingText = "Cargando usuarios...";

        public async Task InicializarAsync()
        {
            IsLoading = true;

            await CargarUsuariosAsync();

            IsLoading = false;
        }

        public int Id { get; set; }

        // ======================
        // LISTAS
        // ======================
        [ObservableProperty]
        private ObservableCollection<UserProfile> usuarios = new();

        private List<UserProfile> todosUsuarios = new();

        // ======================
        // TOGGLE UI
        // ======================
        [ObservableProperty]
        private bool mostrarEliminados;

        partial void OnMostrarEliminadosChanged(bool value)
        {
            AplicarFiltro();
        }

        [RelayCommand]
        public void CancelarCambios()
        {
            Debug.WriteLine("cancelando");
            todosUsuarios = snapshot.Select(u => u.Clone()).ToList();
            AplicarFiltro();
        }

        [RelayCommand]
        public async Task GuardarCambiosAsync()
        {
            Debug.WriteLine("guardando");
            try
            {
                await _db.OpenConnectionAsync();

                foreach (var user in todosUsuarios)
                {
                    var cmd = new MySqlCommand(@"
                                                UPDATE usuarios
                                                SET activo = @activo
                                                WHERE id = @id
                                                limit 1000000
                                            ", _db.Connection);

                    cmd.Parameters.AddWithValue("@activo", user.Activo);
                    cmd.Parameters.AddWithValue("@id", user.Id);

                    await cmd.ExecuteNonQueryAsync();
                }

                await _db.CloseConnectionAsync();

                snapshot = todosUsuarios.Select(u => u.Clone()).ToList();

                IsLoading = true;

                await CargarUsuariosAsync();

                IsLoading = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        // ======================
        // LOAD
        // ======================
        [RelayCommand]
        public async Task CargarUsuariosAsync()
        {
            try
            {
                IsLoading = true;
                LoadingText = "Cargando usuarios...";

                await _db.OpenConnectionAsync();

                var query = @"
            SELECT id, cuenta_cf, nombre, apellido_paterno, estado, genero, tipo, curso_actual, activo
            FROM usuarios
            WHERE cuenta_cf IS NOT NULL
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

                var cmd = new MySqlCommand(query, _db.Connection);

                var reader = await cmd.ExecuteReaderAsync();

                var lista = new List<UserProfile>();

                while (await reader.ReadAsync())
                {
                    var user = new UserProfile
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("id"))
                            ? 0
                            : reader.GetInt32("id"),

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
                        : reader.GetInt32("curso_actual"),

                        Activo = reader.IsDBNull(reader.GetOrdinal("activo"))
                        ? true
                        : reader.GetBoolean("activo")
                    };

                    lista.Add(user);

                    Debug.WriteLine(
                        $"USER => {user.Handle} | {user.FirstName} {user.LastName} | " +
                        $"Estado:{user.Estado} | Sexo:{user.Sexo} | Tipo:{user.Tipo} | Curso:{user.Curso}"
                    );
                }

                await reader.CloseAsync();
                await _db.CloseConnectionAsync();

                todosUsuarios = lista;

                snapshot = lista.Select(u => u.Clone()).ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AplicarFiltro();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                LoadingText = "Error al cargar usuarios";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ======================
        // FILTRO
        // ======================
        private void AplicarFiltro()
        {
            var data = todosUsuarios.AsEnumerable();

            if (!MostrarEliminados)
                data = data.Where(u => u.Activo);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Usuarios = new ObservableCollection<UserProfile>(data);
            });
        }

        // ======================
        // TOGGLE ACTIVO (BORRADO LÓGICO)
        // ======================
        [RelayCommand]
        public async Task CambiarEstadoAsync(UserProfile user)
        {
            if (user == null) return;

            try
            {
                await _db.OpenConnectionAsync();

                var cmd = new MySqlCommand(@"
                    UPDATE usuarios
                    SET activo = @activo
                    WHERE cuenta_cf = @handle
                ", _db.Connection);

                cmd.Parameters.AddWithValue("@activo", user.Activo);
                cmd.Parameters.AddWithValue("@handle", user.Handle);

                await cmd.ExecuteNonQueryAsync();

                await _db.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizar activo: {ex.Message}");
            }
        }
    }
}