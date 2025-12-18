using Dapper;
using Microsoft.Data.SqlClient;
using SharenoteGainsight.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace SharenoteGainsight.DataAccess.Repository
{
    public class StaffRepository : IStaffRepository
    {
        // Default command timeout in seconds (tunable)
        private const int DefaultCommandTimeoutSeconds = 180;

        // Static constructor registers a Dapper type map for StaffRecord that normalizes column names.
        static StaffRepository()
        {
            // Build a dictionary of normalized property names -> PropertyInfo for StaffRecord
            var propMap = typeof(StaffRecord)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => NormalizeName(p.Name), p => p, StringComparer.OrdinalIgnoreCase);

            SqlMapper.SetTypeMap(
                typeof(StaffRecord),
                new CustomPropertyTypeMap(
                    typeof(StaffRecord),
                    (type, columnName) =>
                    {
                        if (string.IsNullOrWhiteSpace(columnName))
                            return null;

                        var normalized = NormalizeName(columnName);
                        return propMap.TryGetValue(normalized, out var prop) ? prop : null;
                    })
            );
        }

        public async Task<List<StaffRecord>> GetStaffAsync(string sql, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL must be provided.", nameof(sql));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                // Let Dapper map columns to StaffRecord using the CustomPropertyTypeMap registered above.
                var rows = await conn.QueryAsync<StaffRecord>(
                    sql,
                    commandType: CommandType.Text,
                    commandTimeout: DefaultCommandTimeoutSeconds
                ).ConfigureAwait(false);

                // Materialize into a List while keeping order
                return rows?.AsList() ?? new List<StaffRecord>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error occurred while fetching staff data from database.", ex);
            }
        }

        // Normalization: remove spaces and underscores and lower-case for stable comparisons.
        private static string NormalizeName(string name)
            => string.IsNullOrEmpty(name)
                ? string.Empty
                : name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }
}
