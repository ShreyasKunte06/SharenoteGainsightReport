using CsvHelper;
using CsvHelper.Configuration;
using SharenoteGainsight.Domain;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
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

                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                });

                csv.Context.RegisterClassMap<StaffRecordMap>();

                // Write header
                csv.WriteHeader<StaffRecord>();
                csv.NextRecord();

                // Write rows (empty collection = header only CSV, which is valid)
                csv.WriteRecords(records);

                await writer.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Let caller decide how to log / handle
                throw new InvalidOperationException(
                    $"Failed to write CSV file at path '{path}'.", ex);
            }
        }
    }
}
