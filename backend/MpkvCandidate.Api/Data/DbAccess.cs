using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MpkvCandidate.Api.Data
{
    /// <summary>
    /// Low-level database helper — wraps Dapper calls to stored procedures.
    /// Mirrors the pattern used in the original Admission.Data project.
    /// </summary>
    public class DbAccess
    {
        private readonly string _connectionString;

        public DbAccess(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        // ── Execute a SP that returns rows as DataTable ──────────────────────
        public DataTable GetDataTable(string spName, DynamicParameters? param = null)
        {
            using var conn = CreateConnection();
            using var reader = conn.ExecuteReader(spName, param, commandType: CommandType.StoredProcedure);
            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        // ── Execute a SP that returns rows as DataSet (multiple tables) ──────
        public DataSet GetDataSet(string spName, DynamicParameters? param = null)
        {
            using var conn = CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };

            if (param != null)
            {
                foreach (var name in param.ParameterNames)
                {
                    cmd.Parameters.AddWithValue(name, param.Get<object>(name) ?? DBNull.Value);
                }
            }

            var ds = new DataSet();
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(ds);
            return ds;
        }

        // ── Execute a SP that returns a single scalar value ──────────────────
        public object? ExecuteScalar(string spName, DynamicParameters? param = null)
        {
            using var conn = CreateConnection();
            return conn.ExecuteScalar(spName, param, commandType: CommandType.StoredProcedure);
        }

        // ── Execute a SP with no return value ────────────────────────────────
        public void ExecuteNonQuery(string spName, DynamicParameters? param = null)
        {
            using var conn = CreateConnection();
            conn.Execute(spName, param, commandType: CommandType.StoredProcedure);
        }

        // ── Execute a SP that returns strongly-typed list ────────────────────
        public IEnumerable<T> Query<T>(string spName, DynamicParameters? param = null)
        {
            using var conn = CreateConnection();
            return conn.Query<T>(spName, param, commandType: CommandType.StoredProcedure);
        }

        // ── Execute a SP that returns a single row ───────────────────────────
        public T? QuerySingleOrDefault<T>(string spName, DynamicParameters? param = null)
        {
            using var conn = CreateConnection();
            return conn.QuerySingleOrDefault<T>(spName, param, commandType: CommandType.StoredProcedure);
        }
    }
}
