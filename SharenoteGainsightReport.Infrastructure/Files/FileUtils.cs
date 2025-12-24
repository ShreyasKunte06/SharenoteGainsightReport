using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsight.Infrastructure.Files
{
    /// <summary>
    /// Provides helper methods for safely moving files between folders
    /// such as Archive and Failed directories.
    ///
    /// This class centralizes file move logic so that error handling,
    /// directory creation, and overwrite behavior are consistent
    /// across the application.
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Moves the specified file to the Archive folder.
        ///
        /// If the destination folder does not exist, it is created.
        /// If a file with the same name already exists in the archive,
        /// it is overwritten.
        /// </summary>
        /// <param name="filePath">Full path of the file to archive.</param>
        /// <param name="archiveFolder">Destination archive directory.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when filePath or archiveFolder is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the file cannot be moved due to IO or permission errors.
        /// </exception>
        public static void MoveToArchive(string filePath, string archiveFolder)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (string.IsNullOrWhiteSpace(archiveFolder))
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

        /// <summary>
        /// Moves the specified file to the Failed folder.
        ///
        /// This method is typically used when processing or uploading
        /// a file fails after all retry attempts.
        /// </summary>
        /// <param name="filePath">Full path of the file to move.</param>
        /// <param name="failedFolder">Destination failed directory.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when filePath or failedFolder is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the file cannot be moved due to IO or permission errors.
        /// </exception>
        public static void MoveToFailed(string filePath, string failedFolder)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            if (string.IsNullOrWhiteSpace(failedFolder))
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
