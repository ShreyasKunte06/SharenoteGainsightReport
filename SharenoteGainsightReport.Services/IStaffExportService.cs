using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Services
{
    public interface IStaffExportService
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);

    }
}
