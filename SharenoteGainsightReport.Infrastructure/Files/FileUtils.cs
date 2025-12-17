using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Infrastructure.Files
{
    public static class FileUtils
    {
        public static void MoveToArchive(string filePath, string archiveFolder)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Archive Folder must be provided.", nameof(archiveFolder));
            try
            {
                Directory.CreateDirectory(archiveFolder);
                var dest = Path.Combine(archiveFolder, Path.GetFileName(filePath));
                File.Move(filePath, dest, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to move file to archive. {ex.Message}", ex);
            }
        }

        public static void MoveToFailed(string filePath, string failedFolder)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Failed Folder must be provided.", nameof(failedFolder));
            try
            {
                Directory.CreateDirectory(failedFolder);
                var dest = Path.Combine(failedFolder, Path.GetFileName(filePath));
                File.Move(filePath, dest, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to move file to failed folder. {ex.Message}", ex);
            }
        }
    }
}
