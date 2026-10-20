namespace Vardiologio.Application.Parameters;

/// <summary>
/// CRUD (with soft-delete) for the supervisors who sign the individual monthly sheet, and
/// which employees each one signs for (Employee.SignatoryId).
/// </summary>
public interface ISignatoryService
{
	/// <summary>All supervisors, ordered by name. includeInactive=false hides soft-deleted ones.</summary>
	Task<IReadOnlyList<SignatoryItem>> GetAllAsync(bool includeInactive);

	/// <summary>Creates a new supervisor (always active); returns the new Id.</summary>
	Task<int> CreateAsync(SignatoryItem item);

	/// <summary>Updates role, name and title. IsActive is changed only via SetActiveAsync.</summary>
	Task UpdateAsync(SignatoryItem item);

	/// <summary>Soft-delete (false) or restore (true). The employees keep pointing at it.</summary>
	Task SetActiveAsync(int id, bool isActive);

	/// <summary>All active employees, alphabetical, each with the supervisor it has now.</summary>
	Task<IReadOnlyList<SignatoryEmployee>> GetEmployeesAsync();

	/// <summary>
	/// Makes <paramref name="signatoryId"/> the supervisor of exactly <paramref name="employeeIds"/>
	/// among the active employees: the listed ones get it (taken from whoever they had before),
	/// and active employees that had it but are no longer listed are left without one.
	/// </summary>
	Task SetEmployeesAsync(int signatoryId, IReadOnlyCollection<int> employeeIds);

	/// <summary>The supervisor of one employee (null when none is set).</summary>
	Task<int?> GetEmployeeSignatoryIdAsync(int employeeId);
}
