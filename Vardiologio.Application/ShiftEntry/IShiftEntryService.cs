namespace Vardiologio.Application.ShiftEntry;

/// <summary>Loads and saves a single employee's monthly shift entries (the daily entry screen).</summary>
public interface IShiftEntryService
{
	/// <summary>Employees for the selector (ordered, with a display label).</summary>
	Task<IReadOnlyList<EmployeeListItem>> GetEmployeesAsync();

	/// <summary>
	/// Shift/status codes for the per-day dropdown: the active ones, plus any inactive code
	/// in <paramref name="alsoInclude"/> (codes already used in the loaded month), so an
	/// existing entry of a retired code is still shown instead of an empty "—".
	/// </summary>
	Task<IReadOnlyList<ShiftCodeOption>> GetShiftCodesAsync(IReadOnlyCollection<int>? alsoInclude = null);

	/// <summary>Builds the month grid for one employee: every day, pre-filled where an entry exists.</summary>
	Task<MonthEntry> LoadMonthAsync(int employeeId, int year, int month);

	/// <summary>Persists the month with upsert semantics (add / update / delete per day).</summary>
	Task SaveMonthAsync(MonthEntry entry);

	/// <summary>
	/// Checks the (possibly unsaved) month: per-day warnings and hours per category with the
	/// monthly limits. See <see cref="MonthEntryRules"/>. Never blocks saving.
	/// </summary>
	Task<MonthCheck> CheckMonthAsync(MonthEntry entry);
}