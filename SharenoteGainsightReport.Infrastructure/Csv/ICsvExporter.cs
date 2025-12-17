using SharenoteGainsight.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Infrastructure.Csv
{
    public interface ICsvExporter
    {
        Task<bool> WriteToCsvAsync(IEnumerable<StaffRecord> records, string path);
    }
}
