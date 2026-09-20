namespace Vardiologio.Infrastructure.Persistence.Seeding;

/// <summary>
/// Shape of one employee row read from the seed JSON file.
/// Kept separate from the Employee entity because Speciality is a name here
/// (resolved to a foreign key at seed time), not an id.
/// </summary>
public record EmployeeSeed(string LastName, string FirstName, string Speciality);