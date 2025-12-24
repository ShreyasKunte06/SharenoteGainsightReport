using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharenoteGainsight.DataAccess;
using SharenoteGainsight.DataAccess.Repository;
using SharenoteGainsight.Domain;
using SharenoteGainsight.Infrastructure.Csv;
using SharenoteGainsight.Infrastructure.Files;
using SharenoteGainsight.Infrastructure.Sftp;
using SharenoteGainsightReport.Domain.Enum;
using SharenoteGainsightReport.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharenoteGainsight.Services
{
    /// <summary>
    /// Orchestrates the end-to-end staff export workflow for ShareNote.
    /// 
    /// Responsibilities:
    /// - Validate execution date (quarter start rule)
    /// - Fetch staff data from database
    /// - Generate CSV report
    /// - Upload report to SFTP with retry handling
    /// - Move files to Archive or Failed folders based on outcome
    /// </summary>
    public class StaffExportService : IStaffExportService
    {
        private readonly ILogger<StaffExportService> _logger;
        private readonly IConfiguration _config;
        private readonly IStaffRepository _staffRepo;
        private readonly ICsvExporter _csv;
        private readonly ISftpService _sftp;

        /// <summary>
        /// Defines valid months for quarter start execution (Jan, Apr, Jul, Oct).
        /// Job runs only on the 1st day of these months.
        /// </summary>
        private static readonly int[] QuarterStartMonths = { 1, 4, 7, 10 };

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
                // Validate whether today is an allowed execution date
                if (!IsValidRunDate())
                {
                    _logger.LogInformation("Today is not configured as a run date. Exiting.");
                    return;
                }

                _logger.LogInformation("Starting ShareNote Staff Export...");

                // Root path for all file system operations
                string rootPath = _config["Paths:RootPath"];
                if (string.IsNullOrWhiteSpace(rootPath))
                    throw new InvalidOperationException("Paths:RootPath is not configured");

                var storedProc = _config["Sql:StaffStoredProcedure"];
                if (string.IsNullOrWhiteSpace(storedProc))
                {
                    _logger.LogError("Stored procedure name not configured (Sql:StaffStoredProcedure)");
                    return;
                }

                var connectionString = _config["Sql:ConnectionString"];
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError("SQL connection string not configured");
                    return;
                }

                var staffRecords = await _staffRepo.GetStaffAsync(StoredProcedure.dbo_usp_GetProviderListGainsight, connectionString);
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
                _logger.LogInformation("CSV file written to {path}", localPath);

                int maxAttempts = 3;
                var retryDelay = TimeSpan.FromSeconds(5);

                var uploadResult = await TryUploadWithRetriesAsync(
                    localPath,
                    fileName,
                    maxAttempts,
                    retryDelay);

                if (uploadResult.Success)
                {
                    _logger.LogInformation(
                        "Upload successful after {attempts} attempt(s). Moving file to Archive.",
                        uploadResult.AttemptsMade);

                    var archiveFolder = Path.Combine(rootPath, "Archive");
                    FileUtils.MoveToArchive(localPath, archiveFolder);
                    _logger.LogInformation(
                        "File {file} Upload successful to Archive folder {archiveFolder}",
                        fileName, archiveFolder);
                }
                else
                {
                    _logger.LogError(
                        "Upload failed after {attempts} attempts. Moving file to Failed.",
                        uploadResult.AttemptsMade);

                    if (uploadResult.LastException != null)
                    {
                        _logger.LogError(
                            uploadResult.LastException,
                            "Last exception during upload for file {file}",
                            fileName);
                    }

                    var failedFolder = Path.Combine(rootPath, "Failed");
                    FileUtils.MoveToFailed(localPath, failedFolder);
                    _logger.LogInformation(
                        "File {file} moved to Failed folder {failedFolder}",
                        fileName, failedFolder);
                }

                _logger.LogInformation("Staff export job finished.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in StaffExportService.ExecuteAsync");
                throw;
            }
        }

        /// <summary>
        /// Determines whether the current date is a valid execution date.
        /// Job runs only on the first day of each quarter.
        /// </summary>
        /// <returns>
        /// <c>true</c> if today is a quarter start date; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidRunDate()
        {
            //var today = DateTime.Today; commented out for testing
            var today = new DateTime(2024, 7, 1); // For testing purposes
            return today.Day == 1 && QuarterStartMonths.Contains(today.Month);
        }

        /// <summary>
        /// Attempts to upload a file to SFTP with retry logic.
        /// Handles transient failures gracefully and captures the last exception.
        /// </summary>
        /// <param name="localPath">Local file path to upload.</param>
        /// <param name="fileName">Target file name on SFTP.</param>
        /// <param name="maxAttempts">Maximum number of retry attempts.</param>
        /// <param name="retryDelay">Delay between retry attempts.</param>
        /// <returns>
        /// An <see cref="UploadResult"/> describing the final upload outcome.
        /// </returns>
        private async Task<UploadResult> TryUploadWithRetriesAsync(
            string localPath,
            string fileName,
            int maxAttempts,
            TimeSpan retryDelay)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Upload attempt {attempt}/{max} for file {file}",
                        attempt, maxAttempts, fileName);

                    bool uploaded = await _sftp.UploadFileAsync(localPath, fileName);

                    if (uploaded)
                    {
                        _logger.LogInformation(
                            "Upload succeeded on attempt {attempt} for file {file}",
                            attempt, fileName);

                        return new UploadResult
                        {
                            Success = true,
                            AttemptsMade = attempt
                        };
                    }

                    _logger.LogWarning(
                        "Upload attempt {attempt} returned false for file {file}",
                        attempt, fileName);
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    _logger.LogError(
                        ex,
                        "Upload attempt {attempt} threw exception for file {file}",
                        attempt, fileName);
                }

                // Delay before next attempt (except last)
                if (attempt < maxAttempts)
                {
                    await Task.Delay(retryDelay);
                }
            }

            // All retries exhausted
            return new UploadResult
            {
                Success = false,
                AttemptsMade = maxAttempts,
                LastException = lastException
            };
        }
    }
}
