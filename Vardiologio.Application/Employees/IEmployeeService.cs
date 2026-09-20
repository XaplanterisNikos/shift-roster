namespace Vardiologio.Application.Employees;

/// <summary>CRUD (with soft-delete) for employees, plus lookup lists for the form.</summary>
public interface IEmployeeService
{
	/// <summary>Specialities to drive the category selector.</summary>
	Task<IReadOnlyList<LookupOption>> GetSpecialitiesAsync();

	/// <summary>Employees of one specialty. When includeInactive is false, soft-deleted ones are hidden.</summary>
	Task<IReadOnlyList<EmployeeListItem>> GetBySpecialityAsync(int specialityId, bool includeInactive);

	/// <summary>One employee for editing (works for inactive too).</summary>
	Task<EmployeeDetail?> GetAsync(int id);

	/// <summary>All dropdown lists for the form.</summary>
	Task<EmployeeLookups> GetLookupsAsync();

	/// <summary>Creates a new employee; returns the new Id.</summary>
	Task<int> CreateAsync(EmployeeDetail detail);

	/// <summary>Updates an existing employee's editable fields.</summary>
	Task UpdateAsync(EmployeeDetail detail);

	/// <summary>Soft-delete (isActive=false) or restore (true).</summary>
	Task SetActiveAsync(int id, bool isActive);
}