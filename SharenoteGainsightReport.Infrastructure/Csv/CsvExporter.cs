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
    /// <summary>
    /// CsvHelper mapping configuration for <see cref="StaffRecord"/>.
    ///
    /// This class defines the exact column headers and their order
    /// in the generated CSV file. Column names are intentionally
    /// matched to the format expected by downstream consumers
    /// (e.g., Gainsight), including spaces and uppercase headers.
    /// </summary>
    public sealed class StaffRecordMap : ClassMap<StaffRecord>
    {
        /// <summary>
        /// Initializes the CSV column mappings for StaffRecord.
        /// Each property is explicitly mapped to avoid reliance
        /// on reflection-based defaults and to ensure stable output.
        /// </summary>
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

    /// <summary>
    /// Responsible for exporting staff data into a CSV file.
    ///
    /// This class encapsulates all CSV-related logic, including:
    /// - Directory creation
    /// - CsvHelper configuration
    /// - Header and record writing
    /// - Safe asynchronous file I/O
    ///
    /// Errors are intentionally not logged here and instead
    /// surfaced to the caller via a boolean return value,
    /// allowing higher-level services to decide how to react.
    /// </summary>
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