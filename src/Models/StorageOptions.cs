namespace Backend.Models;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string GeneratedFilesPath { get; set; } = "GeneratedFiles";
}
