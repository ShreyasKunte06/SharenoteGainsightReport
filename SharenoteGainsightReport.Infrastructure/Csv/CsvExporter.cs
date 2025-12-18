using CsvHelper;
using CsvHelper.Configuration;
using SharenoteGainsight.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Infrastructure.Csv
{
    public sealed class StaffRecordMap : ClassMap<StaffRecord>
    {
        public StaffRecordMap()
        {
            Map(m => m.FName).Name("FName");
            Map(m => m.LName).Name("LName");
            Map(m => m.Email).Name("Email");
            Map(m => m.AccountName).Name("ACCOUNT NAME");
            Map(m => m.Product).Name("PRODUCT");
            Map(m => m.PlatformId).Name("PLATFORM ID");
            Map(m => m.Role).Name("Role");
            Map(m => m.Phone).Name("Phone");
            Map(m => m.Status).Name("Status");
        }
    }

    public class CsvExporter : ICsvExporter
    {
        // Use a larger buffer for file I/O to reduce small writes
        private const int DefaultBufferSize = 64 * 1024;

        public async Task<bool> WriteToCsvAsync(IEnumerable<StaffRecord> records, string path)
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("CSV path must be provided.", nameof(path));

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Open file for asynchronous writes
                await using var stream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    DefaultBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await using var writer = new StreamWriter(stream, Encoding.UTF8, DefaultBufferSize);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,
                    // You can configure more options here (Delimiter, Quote, ShouldQuote, etc.)
                };

                await using var csv = new CsvWriter(writer, config);

                // Register mapping
                csv.Context.RegisterClassMap<StaffRecordMap>();

                csv.WriteHeader<StaffRecord>();
                csv.NextRecord();

                // Write records asynchronously (CsvHelper supports async write APIs)
                await csv.WriteRecordsAsync(records);

                await writer.FlushAsync();
                return true;
            }
            catch
            {
                // Return false so callers (like StaffExportService) can decide how to log/handle the failure.
                return false;
            }
        }
    }
}