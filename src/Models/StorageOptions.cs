namespace DailyOneRosterFile.Api.Models;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string GeneratedFilesPath { get; set; } = "GeneratedFiles";
    public string TokenSecret { get; set; } = "default-secret-key-change-in-production";

    // Blob Storage / MinIO Settings
    public bool UseMinio { get; set; } = false;
    public string MinioEndpoint { get; set; } = string.Empty;
    public string MinioAccessKey { get; set; } = string.Empty;
    public string MinioSecretKey { get; set; } = string.Empty;
    public string MinioBucketName { get; set; } = "generated-files";
    public string BlobConnectionString { get; set; } = string.Empty;
    public string BlobContainerName { get; set; } = "generated-files";
}
