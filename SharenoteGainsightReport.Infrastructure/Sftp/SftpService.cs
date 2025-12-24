using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Infrastructure.Sftp
{
    public class SftpService : ISftpService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SftpService> _logger;

        public SftpService(IConfiguration config, ILogger<SftpService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a local file to the configured SFTP server.
        /// 
        /// The file is uploaded into a quarter-based remote directory (Q1, Q2, Q3, Q4).
        /// The method returns a boolean indicating success or failure; 
        /// exceptions are logged and handled internally.
        /// </summary>
        /// <param name="localFilePath">Full path of the local file to upload.</param>
        /// <param name="remoteFileName">Name of the file on the remote SFTP server.</param>
        /// <returns>True if upload succeeds; otherwise false.</returns>
        public async Task<bool> UploadFileAsync(string localFilePath, string remoteFileName)
        {
            if (string.IsNullOrWhiteSpace(localFilePath))
                throw new ArgumentException(nameof(localFilePath));

            if (!File.Exists(localFilePath))
                throw new FileNotFoundException("Local file not found.", localFilePath);

            if (string.IsNullOrWhiteSpace(remoteFileName))
                throw new ArgumentException(nameof(remoteFileName));

            // Determine quarter folder
            int month = DateTime.Now.Month;

            string currentQuarter = $"Q{((month - 1) / 3) + 1}"; //SFTP directory structure expects a string folder name like "Q1", "Q2", etc.

            string sftpHost = _config["SFTP:Host"] ?? throw new InvalidOperationException("SFTP:Host missing");
            string sftpUser = _config["SFTP:UserName"] ?? throw new InvalidOperationException("SFTP:UserName missing");
            string sftpPass = _config["SFTP:Password"] ?? throw new InvalidOperationException("SFTP:Password missing");
            int sftpPort = int.TryParse(_config["SFTP:Port"], out var p) ? p : 22;
            string remoteDirTemplate = _config["SFTP:RemoteDirectory"] ?? "TEST/Sharenote/{0}";
            string remoteDirectory = string.Format(remoteDirTemplate, currentQuarter);

            string remoteFilePath = $"{remoteDirectory.TrimEnd('/')}/{remoteFileName}";

            _logger.LogInformation("Connecting to SFTP host {host}:{port}", sftpHost, sftpPort);

            bool isSuccess = false;

            // SSH.NET SftpClient is synchronous; wrap in Task.Run to avoid blocking callers
            await Task.Run(() =>
            {
                using var sftp = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPass);
                try
                {
                    sftp.Connect();
                    _logger.LogInformation("Connected to SFTP.");

                    // Ensure remote directory exists (create recursively)
                    if (!sftp.Exists(remoteDirectory))
                    {
                        _logger.LogInformation("Remote directory {dir} does not exist. Creating...", remoteDirectory);
                        CreateRemoteDirectoryRecursive(sftp, remoteDirectory);
                    }

                    using var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                    _logger.LogInformation("Uploading file to {remote}", remoteFilePath);
                    sftp.UploadFile(fs, remoteFilePath, true);
                    isSuccess = true;
                    _logger.LogInformation("Upload complete.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file to SFTP");
                    isSuccess = false;
                }
                finally
                {
                    if (sftp.IsConnected) sftp.Disconnect();
                }
            }).ConfigureAwait(false);

            return isSuccess;
        }

        /// <summary>
        /// Creates the specified remote directory path recursively on the SFTP server.
        /// 
        /// This ensures that nested directories are created one level at a time,
        /// which is required by SSH.NET.
        /// </summary>
        /// <param name="sftp">Active SFTP client connection.</param>
        /// <param name="path">Remote directory path to create.</param>
        private static void CreateRemoteDirectoryRecursive(SftpClient sftp, string path)
        {
            var parts = path.Trim('/').Split('/');
            string current = "";
            foreach (var p in parts)
            {
                current += "/" + p;
                if (!sftp.Exists(current))
                {
                    sftp.CreateDirectory(current);
                }
            }
        }
    }
}
