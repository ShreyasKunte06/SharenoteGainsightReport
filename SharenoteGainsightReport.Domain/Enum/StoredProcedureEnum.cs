using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsightReport.Domain.Enum
{
    /// <summary>
    /// Logical identifiers for stored procedures.
    /// Do NOT put schema or DB-specific names here.
    /// </summary>
    public enum StoredProcedure
    {
        /// <summary>
        /// Retrieves the provider/staff list required for the Gainsight export process.
        ///
        /// Maps internally to the database stored procedure:
        /// dbo.usp_GetProviderListGainsight
        ///
        /// Used by:
        /// - StaffRepository.GetStaffAsync(...)
        /// - StaffExportService execution flow
        /// </summary>
        dbo_usp_GetProviderListGainsight
    }
}
