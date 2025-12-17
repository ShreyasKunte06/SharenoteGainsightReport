using SharenoteGainsight.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.DataAccess.Repository
{
    public interface IStaffRepository
    {
        /// <summary>
        /// Executes the provided SQL and returns mapped staff records.
        /// This method performs robust mapping to handle SQL column names that may contain spaces or different casing.
        /// </summary>
        Task<List<StaffRecord>> GetStaffAsync(string sql, string connectionString);
    }
}
