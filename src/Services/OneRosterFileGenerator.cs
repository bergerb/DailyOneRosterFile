using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using Backend.Models;
using OneRosterSampleDataGenerator;

namespace Backend.Services
{
    public interface IOneRosterFileGenerator
    {
        Task<string> GenerateDailyFileAsync();
    }

    public class OneRosterFileGenerator : IOneRosterFileGenerator
    {
        private readonly string _storagePath;

        public OneRosterFileGenerator(IOptions<StorageOptions> storageOptions)
        {
            _storagePath = storageOptions.Value.GeneratedFilesPath;
        }

        public Task<string> GenerateDailyFileAsync()
        {
            Directory.CreateDirectory(_storagePath);

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

            var destinationPath = Path.Combine(_storagePath, Path.GetFileName(generatedFile));
            File.Move(generatedFile, destinationPath, true);

            return Task.FromResult(destinationPath);
        }
    }
}
