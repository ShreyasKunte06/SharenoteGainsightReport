using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Domain
{
    /// <summary>
    /// Represents a single staff record retrieved from the ShareNote database
    /// and exported to external systems (e.g., Gainsight) via CSV and SFTP.
    ///
    /// This is a pure domain model:
    /// - No business logic
    /// - No infrastructure concerns
    /// - Used across DataAccess, CSV export, and SFTP workflows
    ///
    /// All properties are nullable to safely handle:
    /// - Missing database values
    /// - Optional columns
    /// - Partial or legacy data
    /// </summary>
    public class StaffRecord
    {
        public string? FName { get; set; }
        public string? LName { get; set; }
        public string? Email { get; set; }
        public string? AccountName { get; set; }
        public string? Product { get; set; }
        public string? PlatformId { get; set; }
        public string? Role { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; }
    }
}
