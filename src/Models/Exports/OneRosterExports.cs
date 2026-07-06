namespace DailyOneRosterFile.Api.Models.Exports;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Org
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class Course
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}