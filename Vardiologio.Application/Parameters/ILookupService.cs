namespace Vardiologio.Application.Parameters;

/// <summary>
/// CRUD (with soft-delete) for the employee lookup tables: specialities,
/// employment types and work positions. The <see cref="LookupKind"/> argument picks the table.
/// </summary>
public interface ILookupService
{
	/// <summary>All rows of one kind, ordered by name. includeInactive=false hides soft-deleted ones.</summary>
	Task<IReadOnlyList<LookupItem>> GetAllAsync(LookupKind kind, bool includeInactive);

	/// <summary>
	/// True if another row of this kind (active or inactive) already has this name.
	/// excludeId = the row being edited (0 when creating), so renaming to its own name is allowed.
	/// </summary>
	Task<bool> NameExistsAsync(LookupKind kind, string name, int excludeId);

	/// <summary>True if another employment type (active or inactive) already has this code.</summary>
	Task<bool> EmploymentTypeCodeExistsAsync(string code, int excludeId);

	/// <summary>
	/// Number of ACTIVE employees that use this row. Call before deactivating:
	/// a value still in use by active employees must not be deactivated.
	/// </summary>
	Task<int> CountActiveEmployeesAsync(LookupKind kind, int id);

	/// <summary>Creates a new row (always active); returns the new Id.</summary>
	Task<int> CreateAsync(LookupKind kind, LookupItem item);

	/// <summary>Updates Name (and Code for employment types). IsActive is changed only via SetActiveAsync.</summary>
	Task UpdateAsync(LookupKind kind, LookupItem item);

	/// <summary>
	/// Soft-delete (isActive=false) or restore (true). Does not re-check usage —
	/// the caller is responsible for CountActiveEmployeesAsync beforehand.
	/// </summary>
	Task SetActiveAsync(LookupKind kind, int id, bool isActive);
}
