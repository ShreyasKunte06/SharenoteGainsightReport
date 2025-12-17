namespace SharenoteGainsight.Infrastructure.Sftp
{
    public interface ISftpService
    {
        Task<bool> UploadFileAsync(string localFilePath, string remoteFileName);
    }
}