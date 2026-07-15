using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using DailyOneRosterFile.Api.Interfaces;
using DailyOneRosterFile.Api.Models.Validation;

namespace DailyOneRosterFile.Api.Services;

public class OneRosterValidator : IOneRosterValidator
{
    private static readonly string[] RequiredFiles =
    [
        "academicsessions.csv",
        "classes.csv",
        "courses.csv",
        "demographics.csv",
        "enrollments.csv",
        "manifest.csv",
        "orgs.csv",
        "users.csv"
    ];

    private static readonly Dictionary<string, int> ExpectedHeaderCounts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["academicsessions.csv"] = 9,
        ["classes.csv"] = 14,
        ["courses.csv"] = 10,
        ["demographics.csv"] = 16,
        ["enrollments.csv"] = 10,
        ["manifest.csv"] = 2,
        ["orgs.csv"] = 7,
        ["users.csv"] = 18
    };

    private static readonly Dictionary<string, string[]> ExpectedHeaders = new(StringComparer.OrdinalIgnoreCase)
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

    private static readonly string[] InvalidCharacterPatterns = ["???"];

    private static readonly Regex DateTimeRegex = new(
        @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$",
        RegexOptions.Compiled);

    private static readonly Regex DateRegex = new(
        @"^\d{4}-\d{2}-\d{2}$",
        RegexOptions.Compiled);

    public ValidationResult Validate(Stream zipStream, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult
        {
            ValidatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            CheckForValidZipOrExit(archive, result);
            CheckForAllValidFilesInZip(archive, result);
            CheckManifestVersions(archive, result);
            CheckForEmptyFiles(archive, result);
            CheckForAllFilesHavingValidHeaders(archive, result);
            CheckForDuplicateHeaders(archive, result);
            CheckForValidHeaderOrder(archive, result);
            CheckForValidDateFormat(archive, result);
            CheckAllFilesForInvalidCharWarnings(archive, result);

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"An error occurred while validating the file: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    private static void CheckForValidZipOrExit(ZipArchive archive, ValidationResult result)
    {
        if (archive.Entries.Count < RequiredFiles.Length)
        {
            result.Errors.Add("Error: OneRoster file is missing files (number of files is less than expected).");
        }
    }

    private static void CheckForAllValidFilesInZip(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        var fileNames = archive.Entries.Select(e => e.Name.ToLowerInvariant()).ToHashSet();
        var missingFiles = RequiredFiles.Where(f => !fileNames.Contains(f.ToLowerInvariant())).ToList();

        if (missingFiles.Count > 0)
        {
            result.Errors.Add($"Error: OneRoster file is missing required files: {string.Join(", ", missingFiles)}.");
        }
    }

    private static void CheckManifestVersions(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        var manifestEntry = archive.GetEntry("manifest.csv");
        if (manifestEntry is null)
        {
            return;
        }

        using var stream = manifestEntry.Open();
        using var reader = new StreamReader(stream);

        reader.ReadLine(); // Skip header
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 2)
            {
                continue;
            }

            var propertyName = parts[0].Trim().Trim('"');
            var value = parts[1].Trim().Trim('"');

            if (propertyName.Equals("manifest.version", StringComparison.OrdinalIgnoreCase) && value != "1.0")
            {
                result.Errors.Add($"Error: manifest.csv - manifest.version must be '1.0' but was '{value}'.");
            }

            if (propertyName.Equals("oneroster.version", StringComparison.OrdinalIgnoreCase) && value != "1.1")
            {
                result.Errors.Add($"Error: manifest.csv - oneroster.version must be '1.1' but was '{value}'.");
            }
        }
    }

    private static void CheckForEmptyFiles(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.Length == 0)
            {
                result.Errors.Add($"Error: {entry.Name} - File is empty (no data rows).");
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            var headerLine = reader.ReadLine();
            if (headerLine is null)
            {
                result.Errors.Add($"Error: {entry.Name} - File is empty (no header row).");
                continue;
            }

            var hasDataRows = false;
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    hasDataRows = true;
                    break;
                }
            }

            if (!hasDataRows)
            {
                result.Errors.Add($"Error: {entry.Name} - File has no data rows.");
            }
        }
    }

    private static void CheckForAllFilesHavingValidHeaders(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        foreach (var entry in archive.Entries)
        {
            if (!ExpectedHeaderCounts.TryGetValue(entry.Name, out var expectedCount))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var headerLine = reader.ReadLine();
            if (headerLine is null)
            {
                result.Errors.Add($"Error: {entry.Name} - Could not read header line.");
                return;
            }

            var actualCount = headerLine.Split(',').Length;
            if (actualCount != expectedCount)
            {
                result.Errors.Add($"Error: {entry.Name} - Header count should be {expectedCount} but was {actualCount}.");
                return;
            }
        }
    }

    private static void CheckForDuplicateHeaders(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        foreach (var entry in archive.Entries)
        {
            if (!ExpectedHeaderCounts.ContainsKey(entry.Name))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var headerLine = reader.ReadLine();
            if (headerLine is null)
            {
                continue;
            }

            var headers = headerLine.Split(',');
            var seenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                if (!seenHeaders.Add(header))
                {
                    result.Errors.Add($"Error: {entry.Name} - Duplicate header name '{header}' found.");
                    return;
                }
            }
        }
    }

    private static void CheckForValidHeaderOrder(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        foreach (var entry in archive.Entries)
        {
            if (!ExpectedHeaders.TryGetValue(entry.Name, out var expected))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var headerLine = reader.ReadLine();
            if (headerLine is null)
            {
                continue;
            }

            var actual = headerLine.Split(',');

            for (var i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            {
                if (!string.Equals(expected[i], actual[i], StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"Error: {entry.Name} - Header at position {i + 1} should be '{expected[i]}' but was '{actual[i]}'.");
                    return;
                }
            }
        }
    }

    private static void CheckForValidDateFormat(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        var dateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "startDate", "endDate", "birthDate", "beginDate", "endDate", "assignDate", "dueDate"
        };

        var dateTimeFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dateLastModified"
        };

        foreach (var entry in archive.Entries)
        {
            if (!ExpectedHeaders.ContainsKey(entry.Name))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var headerLine = reader.ReadLine();
            if (headerLine is null)
            {
                continue;
            }

            var headers = headerLine.Split(',');
            var headerToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                headerToIndex[headers[i]] = i;
            }

            var rowCount = 1;
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var fields = line.Split(',');
                foreach (var dateField in dateFields)
                {
                    if (headerToIndex.TryGetValue(dateField, out var index) && index < fields.Length)
                    {
                        var value = fields[index].Trim().Trim('"');
                        if (!string.IsNullOrEmpty(value) && LooksLikeDateAttempt(value) && !DateRegex.IsMatch(value))
                        {
                            result.Errors.Add($"Error: {entry.Name} - Row #{rowCount} field '{dateField}' has invalid date format '{value}'. Expected format: YYYY-MM-DD.");
                            return;
                        }
                    }
                }

                foreach (var dateTimeField in dateTimeFields)
                {
                    if (headerToIndex.TryGetValue(dateTimeField, out var index) && index < fields.Length)
                    {
                        var value = fields[index].Trim().Trim('"');
                        if (!string.IsNullOrEmpty(value) && LooksLikeDateTimeAttempt(value) && !DateTimeRegex.IsMatch(value))
                        {
                            result.Errors.Add($"Error: {entry.Name} - Row #{rowCount} field '{dateTimeField}' has invalid datetime format '{value}'. Expected format: YYYY-MM-DDTHH:MM:SS.sssZ.");
                            return;
                        }
                    }
                }

                rowCount++;
            }
        }
    }

    private static void CheckAllFilesForInvalidCharWarnings(ZipArchive archive, ValidationResult result)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            reader.ReadLine(); // Skip header
            var rowCount = 1;
            string? row;
            while ((row = reader.ReadLine()) is not null)
            {
                foreach (var invalidPattern in InvalidCharacterPatterns)
                {
                    if (row.Contains(invalidPattern))
                    {
                        result.Warnings.Add($"Warning: {entry.Name} - Row #{rowCount} contains invalid character(s): {invalidPattern}.");
                    }
                }
                rowCount++;
            }
        }
    }

    private static bool LooksLikeDateAttempt(string value)
    {
        return value.Contains('-') || value.Contains('/');
    }

    private static bool LooksLikeDateTimeAttempt(string value)
    {
        return value.Contains('T') || value.Contains('-');
    }
}
