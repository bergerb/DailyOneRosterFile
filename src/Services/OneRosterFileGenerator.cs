using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models;
using Microsoft.Extensions.Options;
using OneRosterSampleDataGenerator;

namespace DailyOneRosterFile.Api.Services;

public class OneRosterFileGenerator : IOneRosterFileGenerator
{
    private readonly string _storagePath;
    private readonly IStorageService _storage;
    private readonly StorageOptions _storageOptions;

    public OneRosterFileGenerator(IOptions<StorageOptions> storageOptions, IStorageService storage)
    {
        _storageOptions = storageOptions.Value;
        _storagePath = _storageOptions.GeneratedFilesPath;
        _storage = storage;
    }

    public async Task GenerateDailyFileAsync(int schoolCount)
    {
        var generationBasePath = AppContext.BaseDirectory;

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(generationBasePath);

        var generator = new OneRoster(new() { SchoolCount = schoolCount });
        try
        {
            generator.OutputOneRosterZipFile();
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
        }

        var generatedFile = Directory.GetFiles(generationBasePath, "*.zip")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("OneRoster generator did not produce a zip file.");

        var variant = FileVariant.GetFolder(schoolCount);

        if (_storageOptions.UseMinio)
        {
            byte[] content = File.ReadAllBytes(generatedFile);
            var key = $"{variant}/OneRoster.zip";
            await _storage.UploadFileAsync(key, content);
            File.Delete(generatedFile);
        }
        else
        {
            var subfolderPath = Path.Combine(_storagePath, variant);
            Directory.CreateDirectory(subfolderPath);
            var destinationPath = Path.Combine(subfolderPath, "OneRoster.zip");
            File.Move(generatedFile, destinationPath, true);
        }
    }
}
