using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErizosCF.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using ErizosCF.Services;

namespace ErizosCF.ViewModels
{
    public partial class HitosViewModel : ObservableObject
    {
        private readonly DbService _dbService;

        [ObservableProperty]
        private ObservableCollection<Hito> _hitos = new();

        [ObservableProperty]
        private DateTime _fechaDesde = DateTime.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTime _fechaHasta = DateTime.Now;

        [ObservableProperty]
        private string _nuevoTitulo = string.Empty;

        [ObservableProperty]
        private string? _nuevoLugar;

        [ObservableProperty]
        private DateTime _nuevaFecha = DateTime.Now;

        [ObservableProperty]
        private string? _nuevaDescripcion;

        [ObservableProperty]
        private int? _editandoId;

        public HitosViewModel()
        {
            _dbService = new DbService();
            CargarHitos();
        }

        private async void CargarHitos()
        {
            await CargarHitosAsync();
        }

        [RelayCommand]
        private async Task Filtrar()
        {
            await CargarHitosAsync();
        }

        private async Task CargarHitosAsync()
        {
            try
            {
                await _dbService.OpenConnectionAsync();
                var connection = _dbService.Connection;

                if (connection == null) return;

                string query = @"SELECT id, titulo, descripcion, fecha_realizacion, lugar 
                                 FROM hitos 
                                 WHERE fecha_realizacion BETWEEN @desde AND @hasta
                                 ORDER BY fecha_realizacion DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@desde", FechaDesde.Date);
                cmd.Parameters.AddWithValue("@hasta", FechaHasta.Date);

                Hitos.Clear();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Hitos.Add(new Hito
                    {
                        Id = reader.GetInt32("id"),
                        Titulo = reader.GetString("titulo"),
                        Descripcion = reader.IsDBNull("descripcion") ? null : reader.GetString("descripcion"),
                        FechaRealizacion = reader.GetDateTime("fecha_realizacion"),
                        Lugar = reader.IsDBNull("lugar") ? null : reader.GetString("lugar")
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CargarHitosAsync error: {ex.Message}");
            }
            finally
            {
                await _dbService.CloseConnectionAsync();
            }
        }

        [RelayCommand]
        private async Task Guardar()
        {

            Debug.WriteLine("intentando hacer insert");
            if (string.IsNullOrWhiteSpace(NuevoTitulo))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "El título es obligatorio", "OK");
                return;
            }

            try
            {
                await _dbService.OpenConnectionAsync();
                var connection = _dbService.Connection;

                if (connection == null) return;

                if (EditandoId.HasValue)
                {
                    // Actualizar
                    string query = @"UPDATE hitos 
                                     SET titulo = @titulo, 
                                         descripcion = @descripcion, 
                                         fecha_realizacion = @fecha,
                                         lugar = @lugar 
                                     WHERE id = @id";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", EditandoId.Value);
                    cmd.Parameters.AddWithValue("@titulo", NuevoTitulo);
                    cmd.Parameters.AddWithValue("@descripcion", NuevaDescripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fecha", NuevaFecha.Date);
                    cmd.Parameters.AddWithValue("@lugar", NuevoLugar ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insertar
                    string query = @"INSERT INTO hitos (titulo, descripcion, fecha_realizacion, lugar) 
                                     VALUES (@titulo, @descripcion, @fecha, @lugar)";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@titulo", NuevoTitulo);
                    cmd.Parameters.AddWithValue("@descripcion", NuevaDescripcion ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fecha", NuevaFecha.Date);
                    cmd.Parameters.AddWithValue("@lugar", NuevoLugar ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                // Limpiar formulario
                NuevoTitulo = string.Empty;
                NuevoLugar = null;
                NuevaDescripcion = null;
                NuevaFecha = DateTime.Now;
                EditandoId = null;

                // Recargar lista
                Debug.WriteLine("insert hecho");
                await CargarHitosAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Guardar error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo guardar", "OK");
            }
            finally
            {
                await _dbService.CloseConnectionAsync();
            }
        }

        [RelayCommand]
        private async Task Editar(int id)
        {
            var hito = await ObtenerHitoPorId(id);
            if (hito != null)
            {
                NuevoTitulo = hito.Titulo;
                NuevoLugar = hito.Lugar;
                NuevaFecha = hito.FechaRealizacion;
                NuevaDescripcion = hito.Descripcion;
                EditandoId = hito.Id;
            }
        }

        private async Task<Hito?> ObtenerHitoPorId(int id)
        {
            try
            {
                await _dbService.OpenConnectionAsync();
                var connection = _dbService.Connection;

                if (connection == null) return null;

                string query = "SELECT id, titulo, descripcion, fecha_realizacion, lugar FROM hitos WHERE id = @id";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Hito
                    {
                        Id = reader.GetInt32("id"),
                        Titulo = reader.GetString("titulo"),
                        Descripcion = reader.IsDBNull("descripcion") ? null : reader.GetString("descripcion"),
                        FechaRealizacion = reader.GetDateTime("fecha_realizacion"),
                        Lugar = reader.IsDBNull("lugar") ? null : reader.GetString("lugar")
                    };
                }

                return null;
            }
            finally
            {
                await _dbService.CloseConnectionAsync();
            }
        }

        [RelayCommand]
        private async Task Eliminar(int id)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirmar",
                "¿Eliminar este hito?",
                "Sí",
                "No");

            if (!confirm) return;

            try
            {
                await _dbService.OpenConnectionAsync();
                var connection = _dbService.Connection;

                if (connection == null) return;

                string query = "DELETE FROM hitos WHERE id = @id";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                await CargarHitosAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eliminar error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar", "OK");
            }
            finally
            {
                await _dbService.CloseConnectionAsync();
            }
        }
    }
}