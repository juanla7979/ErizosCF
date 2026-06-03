using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace ErizosCF.Services
{
    public class DbService
    {
        private readonly string _connectionString;
        private MySqlConnection? _connection;
        public DbService()
        {
            var server = Environment.GetEnvironmentVariable("erizoscf_db_server");
            var user = Environment.GetEnvironmentVariable("erizoscf_db_user");
            var password = Environment.GetEnvironmentVariable("erizoscf_db_password");
            var database = Environment.GetEnvironmentVariable("erizoscf_db_name");

            bool faltanVariables =
                string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(database);

            if (faltanVariables)
            {
                server = "localhost";
                user = "root";
                password = "root";
                database = "erizosCF";
            }

            _connectionString =
                $"server={server};user={user};password={password};database={database};";
        }

        public async Task OpenConnectionAsync()
        {
            try
            {
                if (_connection == null)
                {
                    _connection = new MySqlConnection(_connectionString);
                }

                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenConnectionAsync error: {ex.Message}");
            }
        }
        public async Task CloseConnectionAsync()
        {
            try
            {
                if (_connection != null &&
                    _connection.State != System.Data.ConnectionState.Closed)
                {
                    await _connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CloseConnectionAsync error: {ex.Message}");
            }
        }
        public MySqlConnection? Connection => _connection;
        public void Dispose()
        {
            try
            {
                CloseConnectionAsync();
                _connection?.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}