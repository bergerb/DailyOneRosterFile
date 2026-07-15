using System.IO.Compression;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models.Validation;
using DailyOneRosterFile.Api.Services;

namespace DailyOneRosterFile.Api.Tests.Services;

public class OneRosterValidatorTests
{
    private readonly OneRosterValidator _validator = new();

    private static readonly Dictionary<string, string[]> RequiredHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["academicsessions.csv"] = ["sourcedId", "status", "dateLastModified", "title", "type", "startDate", "endDate", "parentSourcedId", "schoolYear"],
        ["classes.csv"] = ["sourcedId", "status", "dateLastModified", "title", "grades", "courseSourcedId", "classCode", "classType", "location", "schoolSourcedId", "termSourcedIds", "subjects", "subjectCodes", "periods"],
        ["courses.csv"] = ["sourcedId", "status", "dateLastModified", "schoolYearSourcedId", "title", "courseCode", "grades", "orgSourcedId", "subjects", "subjectCodes"],
        ["demographics.csv"] = ["sourcedId", "status", "dateLastModified", "birthDate", "sex", "americanIndianOrAlaskaNative", "asian", "blackOrAfricanAmerican", "nativeHawaiianOrOtherPacificIslander", "white", "demographicRaceTwoOrMoreRaces", "hispanicOrLatinoEthnicity", "countryOfBirthCode", "stateOfBirthAbbreviation", "cityOfBirth", "publicSchoolResidenceStatus"],
        ["enrollments.csv"] = ["sourcedId", "status", "dateLastModified", "classSourcedId", "schoolSourcedId", "userSourcedId", "role", "primary", "beginDate", "endDate"],
        ["manifest.csv"] = ["propertyName", "value"],
        ["orgs.csv"] = ["sourcedId", "status", "dateLastModified", "name", "type", "identifier", "parentSourcedId"],
        ["users.csv"] = ["sourcedId", "status", "dateLastModified", "enabledUser", "orgSourcedId", "role", "username", "givenName", "familyName", "middleName", "identifier", "email", "sms", "phone", "agentSourcedIds", "grades", "password", "userIds"]
    };

    [Fact]
    public void Validate_ReturnsValidForCorrectFile()
    {
        // Arrange
        using var stream = CreateValidOneRosterZipStream();

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ReturnsInvalidForMissingFiles()
    {
        // Arrange
        using var stream = CreateZipWithMissingFilesStream();

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_ReturnsInvalidForWrongHeaderCount()
    {
        // Arrange
        using var stream = CreateZipWithWrongHeadersStream();

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Header count"));
    }

    [Fact]
    public void Validate_ReturnsWarningsForInvalidCharacters()
    {
        // Arrange
        using var stream = CreateZipWithInvalidCharactersStream();

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.True(result.IsValid); // Warnings don't make it invalid
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("???"));
    }

    [Fact]
    public void Validate_ReturnsValidatedAtTimestamp()
    {
        // Arrange
        using var stream = CreateValidOneRosterZipStream();
        var beforeValidation = DateTimeOffset.UtcNow;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.True(result.ValidatedAt >= beforeValidation);
        Assert.True(result.ValidatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Validate_EmptyZip_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            // Empty archive
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("missing files"));
    }

    [Fact]
    public void Validate_TooFewFiles_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddCsvEntry(archive, "academicsessions.csv", 9);
            AddCsvEntry(archive, "classes.csv", 14);
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("missing files"));
    }

    [Fact]
    public void Validate_InvalidManifestVersion_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFilesWithManifest(archive, "2.0", "1.1");
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("manifest.version must be '1.0'"));
    }

    [Fact]
    public void Validate_InvalidOneRosterVersion_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFilesWithManifest(archive, "1.0", "1.0");
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("oneroster.version must be '1.1'"));
    }

    [Fact]
    public void Validate_EmptyFile_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
            // Override one file to be empty
            var entry = archive.CreateEntry("users.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            // No content written
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("File is empty"));
    }

    [Fact]
    public void Validate_NoDataRows_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
            // Override users.csv with headers only, no data
            var entry = archive.CreateEntry("users.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.WriteLine(string.Join(",", RequiredHeaders["users.csv"]));
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("no data rows"));
    }

    [Fact]
    public void Validate_DuplicateHeaders_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
            // Override users.csv with duplicate headers
            var entry = archive.CreateEntry("users.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            var headers = RequiredHeaders["users.csv"].ToList();
            headers[1] = headers[0]; // Duplicate sourcedId
            writer.WriteLine(string.Join(",", headers));
            writer.WriteLine(string.Join(",", headers.Select((_, i) => $"value{i}")));
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate header name"));
    }

    [Fact]
    public void Validate_WrongHeaderOrder_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
            // Override users.csv with wrong header order (swap first two)
            var entry = archive.CreateEntry("users.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            var headers = RequiredHeaders["users.csv"].ToList();
            (headers[0], headers[1]) = (headers[1], headers[0]); // Swap sourcedId and status
            writer.WriteLine(string.Join(",", headers));
            writer.WriteLine(string.Join(",", headers.Select((_, i) => $"value{i}")));
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Header at position"));
    }

    [Fact]
    public void Validate_InvalidDateFormat_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
            // Override academicsessions.csv with invalid date format
            var entry = archive.CreateEntry("academicsessions.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.WriteLine(string.Join(",", RequiredHeaders["academicsessions.csv"]));
            // Invalid date format: 01/15/2024 instead of 2024-01-15
            writer.WriteLine("1,active,2024-01-01T00:00:00.000Z,Fall Semester,semester,01/15/2024,06/15/2024,,2024");
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid date format"));
    }

    [Fact]
    public void Validate_InvalidDateTimeFormat_ReturnsError()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
            // Override academicsessions.csv with invalid datetime format
            var entry = archive.CreateEntry("academicsessions.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.WriteLine(string.Join(",", RequiredHeaders["academicsessions.csv"]));
            // Invalid datetime format: 2024-01-01 instead of 2024-01-01T00:00:00.000Z
            writer.WriteLine("1,active,2024-01-01,Fall Semester,semester,2024-01-15,2024-06-15,,2024");
        }
        stream.Position = 0;

        // Act
        var result = _validator.Validate(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid datetime format"));
    }

    private static MemoryStream CreateValidOneRosterZipStream()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateZipWithMissingFilesStream()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddCsvEntryWithHeaders(archive, "academicsessions.csv", RequiredHeaders["academicsessions.csv"]);
            AddCsvEntryWithHeaders(archive, "classes.csv", RequiredHeaders["classes.csv"]);
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateZipWithWrongHeadersStream()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);

            // Override users.csv with wrong header count (3 instead of 18)
            var entry = archive.CreateEntry("users.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.WriteLine("id,firstName,lastName"); // Wrong count, should be 18
            writer.WriteLine("1,John,Doe");
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateZipWithInvalidCharactersStream()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddRequiredCsvFiles(archive);

            // Override users.csv with invalid characters
            var entry = archive.CreateEntry("users.csv");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.WriteLine(string.Join(",", RequiredHeaders["users.csv"]));
            writer.WriteLine("1,active,2024-01-01T00:00:00.000Z,true,org1,student,john,John,Doe,,,,,,,");
            writer.WriteLine("2,active,2024-01-01T00:00:00.000Z,true,org1,student,jane,Jane,Smith,,,,,,,???"); // Invalid character
        }
        stream.Position = 0;
        return stream;
    }

    private static void AddRequiredCsvFiles(ZipArchive archive)
    {
        AddRequiredCsvFilesWithManifest(archive, "1.0", "1.1");
    }

    private static void AddRequiredCsvFilesWithManifest(ZipArchive archive, string manifestVersion, string oneRosterVersion)
    {
        AddCsvEntryWithHeaders(archive, "academicsessions.csv", RequiredHeaders["academicsessions.csv"]);
        AddCsvEntryWithHeaders(archive, "classes.csv", RequiredHeaders["classes.csv"]);
        AddCsvEntryWithHeaders(archive, "courses.csv", RequiredHeaders["courses.csv"]);
        AddCsvEntryWithHeaders(archive, "demographics.csv", RequiredHeaders["demographics.csv"]);
        AddCsvEntryWithHeaders(archive, "enrollments.csv", RequiredHeaders["enrollments.csv"]);
        AddManifestEntry(archive, manifestVersion, oneRosterVersion);
        AddCsvEntryWithHeaders(archive, "orgs.csv", RequiredHeaders["orgs.csv"]);
        AddCsvEntryWithHeaders(archive, "users.csv", RequiredHeaders["users.csv"]);
    }

    private static void AddManifestEntry(ZipArchive archive, string manifestVersion, string oneRosterVersion)
    {
        var entry = archive.CreateEntry("manifest.csv");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.WriteLine("propertyName,value");
        writer.WriteLine($"manifest.version,{manifestVersion}");
        writer.WriteLine($"oneroster.version,{oneRosterVersion}");
        writer.WriteLine("file.academicSessions,bulk");
        writer.WriteLine("file.categories,absent");
        writer.WriteLine("file.classResources,absent");
        writer.WriteLine("file.classes,bulk");
        writer.WriteLine("file.courseResources,absent");
        writer.WriteLine("file.courses,bulk");
        writer.WriteLine("file.demographics,bulk");
        writer.WriteLine("file.enrollments,bulk");
        writer.WriteLine("file.lineItems,absent");
        writer.WriteLine("file.orgs,bulk");
        writer.WriteLine("file.resources,absent");
        writer.WriteLine("file.results,absent");
        writer.WriteLine("file.users,bulk");
    }

    private static void AddCsvEntryWithHeaders(ZipArchive archive, string fileName, string[] headers)
    {
        var entry = archive.CreateEntry(fileName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.WriteLine(string.Join(",", headers));
        writer.WriteLine(string.Join(",", headers.Select(GetDefaultValue)));
    }

    private static string GetDefaultValue(string header)
    {
        return header switch
        {
            "startDate" or "endDate" or "birthDate" or "beginDate" or "assignDate" or "dueDate" => "2024-01-15",
            "dateLastModified" => "2024-01-01T00:00:00.000Z",
            "schoolYear" => "2024",
            _ => "value"
        };
    }

    private static void AddCsvEntry(ZipArchive archive, string fileName, int headerCount)
    {
        var entry = archive.CreateEntry(fileName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        var headers = string.Join(",", Enumerable.Range(1, headerCount).Select(i => $"header{i}"));
        writer.WriteLine(headers);
        writer.WriteLine(string.Join(",", Enumerable.Range(1, headerCount).Select(i => $"value{i}")));
    }
}
