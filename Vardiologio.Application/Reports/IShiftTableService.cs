namespace Vardiologio.Application.Reports;

/// <summary>Builds the monthly official shift table ("Πίνακας Βαρδιών Εργασίας").</summary>
public interface IShiftTableService
{
	/// <summary>
	/// Builds the table for the month: every active employee, the codes of the month, the count
	/// per work-shift code and the paid hours (same calculation and limits as the hours report).
	/// </summary>
	Task<ShiftTableModel> BuildAsync(int year, int month);
}
