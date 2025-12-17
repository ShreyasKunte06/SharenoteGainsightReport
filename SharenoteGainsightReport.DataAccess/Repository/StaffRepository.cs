using Dapper;
using Microsoft.Data.SqlClient;
using SharenoteGainsight.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.DataAccess.Repository
{
    public class StaffRepository : IStaffRepository
    {
        public async Task<List<StaffRecord>> GetStaffAsync(string sql, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentException("SQL must be provided", nameof(sql));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("connectionString", nameof(connectionString));

            var result = new List<StaffRecord>();

            try
            {

                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                // Query as dynamic rows for robust mapping (handles odd column names)
                var rows = await conn.QueryAsync(sql).ConfigureAwait(false);

                foreach (var row in rows)
                {
                    // Dapper returns a dynamic object (usually a DapperRow). Convert to dictionary-like access.
                    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    if (row is IDictionary<string, object?> d)
                    {
                        foreach (var kv in d) dict[kv.Key] = kv.Value;
                    }
                    else
                    {
                        // fallback: use reflection to inspect properties
                        var props = row.GetType().GetProperties();
                        foreach (var p in props)
                        {
                            var key = p.Name;
                            var val = p.GetValue(row);
                            dict[key] = val;
                        }
                    }

                    // Helper to find value by multiple possible column name variants
                    static object? GetValue(Dictionary<string, object?> dict, params string[] keys)
                    {
                        foreach (var k in keys)
                        {
                            if (dict.TryGetValue(k, out var v) && v != null) return v;
                        }
                        // try normalized: remove spaces and underscores, lower
                        foreach (var kv in dict)
                        {
                            var norm = kv.Key?.Replace(" ", "").Replace("_", "").ToLowerInvariant();
                            foreach (var k in keys)
                            {
                                var kn = k.Replace(" ", "").Replace("_", "").ToLowerInvariant();
                                if (norm == kn) return kv.Value;
                            }
                        }
                        return null;
                    }

                    var record = new StaffRecord
                    {
                        FName = Convert.ToString(GetValue(dict, "FName", "FName", "fname", "firstName")),
                        LName = Convert.ToString(GetValue(dict, "LName", "lname", "lastName")),
                        Email = Convert.ToString(GetValue(dict, "Email", "email")),
                        AccountName = Convert.ToString(GetValue(dict, "ACCOUNT NAME", "AccountName", "ACCOUNTNAME")),
                        Product = Convert.ToString(GetValue(dict, "PRODUCT", "Product")),
                        PlatformId = Convert.ToString(GetValue(dict, "PLATFORM ID", "PlatformId", "providerid")),
                        Role = Convert.ToString(GetValue(dict, "Role", "role", "position", "job_title")),
                        Phone = Convert.ToString(GetValue(dict, "Phone", "phone", "phone1")),
                        Status = Convert.ToString(GetValue(dict, "Status", "status"))
                    };

                    result.Add(record);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error occurred while fetching staff data from database.", ex);
            }
            return result;
        }
    }
}
