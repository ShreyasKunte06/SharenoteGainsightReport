using Dapper;
using Microsoft.Data.SqlClient;
using SharenoteGainsight.Domain;
using SharenoteGainsightReport.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SharenoteGainsight.DataAccess.Repository
{
    /// <summary>
    /// Repository responsible for retrieving staff/provider data from the database.
    ///
    /// This repository:
    /// - Executes stored procedures using Dapper
    /// - Maps database result sets to <see cref="StaffRecord"/> objects
    /// - Uses a type-safe <see cref="StoredProcedure"/> enum instead of raw strings
    /// - Handles non-standard SQL column names (spaces, underscores, casing)
    /// </summary>
    public class StaffRepository : IStaffRepository
    {
        /// <summary>
        /// Default SQL command timeout (in seconds).
        /// Prevents long-running queries from hanging indefinitely.
        /// </summary>
        private const int DefaultCommandTimeoutSeconds = 180;

        /// <summary>
        /// Static constructor registers a custom Dapper type map for <see cref="StaffRecord"/>.
        ///
        /// WHY THIS EXISTS:
        /// Database column names may contain spaces, underscores, or different casing
        /// (e.g. "ACCOUNT NAME", "PLATFORM_ID").
        ///
        /// This mapping normalizes column names and matches them to C# property names
        /// without requiring SQL aliases or fragile manual mapping logic.
        ///
        /// This configuration runs ONCE per application lifetime.
        /// </summary>
        static StaffRepository()
        {
            var propMap = typeof(StaffRecord)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(
                    p => NormalizeName(p.Name),
                    p => p,
                    StringComparer.OrdinalIgnoreCase);

            SqlMapper.SetTypeMap(
                typeof(StaffRecord),
                new CustomPropertyTypeMap(
                    typeof(StaffRecord),
                    (_, columnName) =>
                        propMap.TryGetValue(NormalizeName(columnName), out var prop)
                            ? prop
                            : null));
        }

        /// <summary>
        /// Executes the specified stored procedure and returns staff records.
        ///
        /// The stored procedure is identified using a strongly-typed enum,
        /// which is resolved to the actual database procedure name internally.
        /// </summary>
        /// <param name="storedProcName">Logical identifier of the stored procedure</param>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>List of <see cref="StaffRecord"/> objects</returns>
        public async Task<List<StaffRecord>> GetStaffAsync(
            StoredProcedure storedProcName,
            string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException(
                    "Connection string must be provided.",
                    nameof(connectionString));

            string procName = ResolveStoredProcedureName(storedProcName);

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                var rows = await conn.QueryAsync<StaffRecord>(
                    procName,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: DefaultCommandTimeoutSeconds
                ).ConfigureAwait(false);

                return rows?.AsList() ?? new List<StaffRecord>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error occurred while executing stored procedure '{procName}'.",
                    ex);
            }
        }

        /// <summary>
        /// Resolves the enum value to the actual database stored procedure name.
        ///
        /// Centralizing this logic prevents string manipulation from leaking
        /// throughout the codebase and makes future changes safer.
        /// </summary>
        private static string ResolveStoredProcedureName(StoredProcedure storedProcedure)
        {
            return storedProcedure switch
            {
                StoredProcedure.dbo_usp_GetProviderListGainsight
                    => "dbo.usp_GetProviderListGainsight",

                _ => throw new ArgumentOutOfRangeException(
                        nameof(storedProcedure),
                        storedProcedure,
                        "Unsupported stored procedure")
            };
        }

        /// <summary>
        /// Normalizes column/property names by removing spaces and underscores
        /// and converting to lower case.
        ///
        /// Used to match SQL column names to C# properties reliably.
        /// </summary>
        private static string NormalizeName(string name) =>
            string.IsNullOrEmpty(name)
                ? string.Empty
                : name.Replace(" ", "")
                      .Replace("_", "")
                      .ToLowerInvariant();
    }
}

