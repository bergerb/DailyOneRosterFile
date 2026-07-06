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

    public async Task<string> GenerateDailyFileAsync()
    {
        var generationBasePath = AppContext.BaseDirectory;

        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(generationBasePath);

        var generator = new OneRoster();
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

        if (_storageOptions.UseMinio)
        {
            byte[] content = File.ReadAllBytes(generatedFile);
            var fileName = Path.GetFileName(generatedFile);
            var uploadedName = await _storage.UploadFileAsync(fileName, content);
            File.Delete(generatedFile);
            return uploadedName;
        }
        else
        {
            Directory.CreateDirectory(_storagePath);
            var destinationPath = Path.Combine(_storagePath, Path.GetFileName(generatedFile));
            File.Move(generatedFile, destinationPath, true);
            return destinationPath;
        }
    }
}
