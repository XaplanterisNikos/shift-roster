namespace Vardiologio.Application.ShiftEntry;

/// <summary>Loads and saves a single employee's monthly shift entries (the daily entry screen).</summary>
public interface IShiftEntryService
{
	/// <summary>Employees for the selector (ordered, with a display label).</summary>
	Task<IReadOnlyList<EmployeeListItem>> GetEmployeesAsync();

	/// <summary>Shift/status codes for the per-day dropdown.</summary>
	Task<IReadOnlyList<ShiftCodeOption>> GetShiftCodesAsync();

	/// <summary>Builds the month grid for one employee: every day, pre-filled where an entry exists.</summary>
	Task<MonthEntry> LoadMonthAsync(int employeeId, int year, int month);

	/// <summary>Persists the month with upsert semantics (add / update / delete per day).</summary>
	Task SaveMonthAsync(MonthEntry entry);
}