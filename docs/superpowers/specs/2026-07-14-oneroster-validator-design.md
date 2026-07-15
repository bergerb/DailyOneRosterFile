# OneRoster Validator Design

## Goal
Add on-demand validation of generated OneRoster 1.1 ZIP files to ensure structural integrity and provide validation results via API.

## Scope
- New `OneRosterValidator` service with 4 validation checks
- New API endpoint to validate stored files
- New API endpoint to retrieve validation results
- Unit tests for validation logic
- Integration with existing file storage (local filesystem or MinIO)

## Decisions
- Implement 4 validation checks from NemesisApp (skip database-dependent threshold check):
  1. ZIP file count validation
  2. Required files present validation
  3. CSV header count validation
  4. Invalid character warnings
- Validation runs on-demand via API endpoint (not automatically after generation)
- Validation results returned as JSON via API endpoint
- No persistent storage of validation results (stateless validation)
- Reuse existing `IStorageService` to access stored ZIP files

## Architecture
### New Components

#### 1. `IOneRosterValidator` Interface
```csharp
public interface IOneRosterValidator
{
    Task<ValidationResult> ValidateAsync(string variant, CancellationToken cancellationToken = default);
}
```

#### 2. `OneRosterValidator` Service
- Implements `IOneRosterValidator`
- Dependencies: `IStorageService`, `ILogger<OneRosterValidator>`
- Performs 4 validation checks sequentially (stops on first error)
- Returns `ValidationResult` with errors and warnings

#### 3. `ValidationResult` Model
```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTimeOffset ValidatedAt { get; set; }
    public string Variant { get; set; }
}
```

#### 4. API Endpoints
- `GET /api/files/validate?variant={small|large}` - Validate a stored file
- `GET /api/files/validation-result?variant={small|large}` - Get validation status (could cache results)

### Modified Components
- `Program.cs`: Register `OneRosterValidator` in DI container
- `FilesController.cs`: Add validation endpoints

## Data Flow
### Validation Request Flow
1. Client sends `GET /api/files/validate?variant=small`
2. Controller retrieves file path via `IStorageService`
3. Controller calls `IOneRosterValidator.ValidateAsync()`
4. Validator opens ZIP file and performs 4 checks
5. Validator returns `ValidationResult` with errors/warnings
6. Controller returns JSON response with validation results

### Validation Checks Flow
1. **ZIP File Count**: Open ZIP, verify entry count >= 8
2. **Required Files Present**: Check for all 8 mandatory CSV files
3. **CSV Headers**: Read first line of each CSV, verify column count matches expected
4. **Invalid Characters**: Scan all rows for known invalid characters (e.g., "???")

## Error Handling
- If file doesn't exist for variant: Return 404 with descriptive message
- If ZIP is corrupted: Return validation error with details
- If CSV parsing fails: Return validation error with file name and line number
- All validation errors captured in `ValidationResult.Errors`
- Validation warnings captured in `ValidationResult.Warnings`

## Testing Strategy
### Unit Tests
1. `OneRosterValidatorTests.cs` - Test each validation check independently
   - Test valid ZIP passes all checks
   - Test missing files detected
   - Test invalid header counts detected
   - Test invalid characters detected as warnings
   - Test ZIP with too few files detected

### Integration Tests
1. Test validation endpoint with valid generated file
2. Test validation endpoint with non-existent variant
3. Test validation endpoint with corrupted ZIP (if possible to create)

## Success Criteria
- Validation endpoint returns accurate results for generated files
- All 4 validation checks work correctly
- API returns proper HTTP status codes (200, 404, 500)
- Unit tests pass with >90% coverage for validator logic
- No impact on existing file generation or download functionality

## Implementation Notes
- Reuse validation constants from NemesisApp (file names, header counts, invalid characters)
- Consider adding validation results to download endpoint response (optional enhancement)
- Validation should be performant (< 1 second for typical files)
- No database required - stateless validation