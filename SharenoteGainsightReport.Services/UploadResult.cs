using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharenoteGainsightReport.Services
{
    /// <summary>
    /// Represents the outcome of an upload operation that may involve retries.
    /// 
    /// This model is used to communicate:
    /// - Whether the upload ultimately succeeded
    /// - How many attempts were made
    /// - The last exception encountered (if any)
    /// </summary>
    public sealed class UploadResult
    {
        /// <summary>
        /// Indicates whether the upload completed successfully.
        /// </summary>
        public bool Success { get; init; }
        /// <summary>
        /// The total number of attempts made to upload the file.
        /// </summary>
        public int AttemptsMade { get; init; }
        /// <summary>
        /// The last exception thrown during the upload process, if applicable.
        /// 
        /// This is useful for logging or diagnostic purposes when all retries fail.
        /// </summary>
        public Exception? LastException { get; init; }
    }
}
