using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharenoteGainsight.DataAccess;
using SharenoteGainsight.DataAccess.Repository;
using SharenoteGainsight.Domain;
using SharenoteGainsight.Infrastructure.Csv;
using SharenoteGainsight.Infrastructure.Files;
using SharenoteGainsight.Infrastructure.Sftp;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharenoteGainsight.Services
{
    public class StaffExportService : IStaffExportService
    {
        private readonly ILogger<StaffExportService> _logger;
        private readonly IConfiguration _config;
        private readonly IStaffRepository _staffRepo;
        private readonly ICsvExporter _csv;
        private readonly ISftpService _sftp;

        public StaffExportService(
            ILogger<StaffExportService> logger,
            IConfiguration config,
            IStaffRepository staffRepo,
            ICsvExporter csv,
            ISftpService sftp)
        {
            _logger = logger;
            _config = config;
            _staffRepo = staffRepo;
            _csv = csv;
            _sftp = sftp;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!IsValidRunDate())
                {
                    _logger.LogInformation("Today is not configured as a run date. Exiting.");
                    return;
                }

                _logger.LogInformation("Starting ShareNote Staff Export...");

                string rootPath = _config["Paths:RootPath"];
                if (string.IsNullOrWhiteSpace(rootPath))
                    throw new InvalidOperationException("Paths:RootPath is not configured");

                // SQL location relative to project path (copied into output by DataAccess project)
                string sqlRelativePath = _config["Sql:SqlFilePath"] ?? Path.Combine("Sql", "GetStaff.sql");
                string sqlPath = Path.Combine(
                    AppContext.BaseDirectory,
                    sqlRelativePath
                );
                if (!File.Exists(sqlPath))
                {
                    _logger.LogError("SQL file not found at {path}", sqlPath);
                    return;
                }

                var sql = await File.ReadAllTextAsync(sqlPath, cancellationToken);
                _logger.LogInformation("Loaded SQL from {path}", sqlPath);

                var connectionString = _config["Sql:ConnectionString"];

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError("SQL connection string template/server/database not configured");
                    return;
                }

                var staffRecords = await _staffRepo.GetStaffAsync(sql, connectionString);
                if (staffRecords == null || !staffRecords.Any())
                {
                    _logger.LogWarning("No staff records returned. Exiting.");
                    return;
                }

                _logger.LogInformation("Fetched {count} staff records.", staffRecords.Count);

                string reportsRelative = _config["Paths:Reports"] ?? "Reports";
                string reportsFolder = Path.GetFullPath(Path.Combine(rootPath, reportsRelative));
                Directory.CreateDirectory(reportsFolder);

                DateTime today = DateTime.Now;
                string fileName = $"ShareNote-{today:yyyy}-{today:MM}-{today:dd}.csv";
                string localPath = Path.Combine(reportsFolder, fileName);

                bool csvOk = await _csv.WriteToCsvAsync(staffRecords, localPath);
                if (!csvOk)
                {
                    _logger.LogError("Failed to write CSV to {path}", localPath);
                    return;
                }

                int maxAttempts = 3; //retry Attempts
                bool uploadSucceeded = false;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Upload attempt {attempt} of {maxAttempts} for file {file}",
                            attempt, maxAttempts, fileName);

                        uploadSucceeded = await _sftp.UploadFileAsync(localPath, fileName);

                        if (uploadSucceeded)
                        {
                            _logger.LogInformation("Upload succeeded on attempt {attempt}", attempt);

                            var archiveFolder = Path.Combine(rootPath, "Archive");
                            FileUtils.MoveToArchive(localPath, archiveFolder);

                            break; // ✅ STOP retrying once successful
                        }

                        _logger.LogWarning(
                            "Upload attempt {attempt} returned false for file {file}",
                            attempt, fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Upload attempt {attempt} failed due to exception for file {file}",
                            attempt, fileName);
                    }

                    // Optional small delay between retries (prevents hammering SFTP)
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }

                if (!uploadSucceeded)
                {
                    _logger.LogError(
                        "All {maxAttempts} upload attempts failed. Moving file to Failed folder.",
                        maxAttempts);

                    var failedFolder = Path.Combine(rootPath, "Failed");
                    FileUtils.MoveToFailed(localPath, failedFolder);
                }

                _logger.LogInformation("Staff export job finished.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in StaffExportService.ExecuteAsync");
                throw;
            }
        }

        private bool IsValidRunDate()
        {
            var today = new DateTime(2025, 1, 1); // simulate Oct 1

            //var today = DateTime.Today;

            // quarters: Jan (1), Apr (4), Jul (7), Oct (10) on day 1
            if (today.Day == 1 && (today.Month == 1 || today.Month == 4 || today.Month == 7 || today.Month == 10))
                return true;

            return false;
        }
    }
}
