using System.ComponentModel.DataAnnotations;

namespace Vardiologio.Application.Hours;

/// <summary>Editable holiday row for the settings screen.</summary>
public class HolidayItem
{
	/// <summary>Primary key (0 for a new row).</summary>
	public int Id { get; set; }

	/// <summary>The holiday date (unique).</summary>
	public DateOnly Date { get; set; }

	// Mirrors IsRequired().HasMaxLength(100) in AppDbContext — SQLite does not enforce the length.
	[Required(ErrorMessage = "Το όνομα είναι υποχρεωτικό.")]
	[StringLength(100, ErrorMessage = "Το όνομα μπορεί να έχει έως 100 χαρακτήρες.")]
	public string Name { get; set; } = "";
}

/// <summary>CRUD for public holidays, plus filling a year from the suggested calendar.</summary>
public interface IHolidayService
{
	/// <summary>Holidays of one year, ordered by date.</summary>
	Task<IReadOnlyList<HolidayItem>> GetByYearAsync(int year);

	/// <summary>True if another row (not <paramref name="excludeId"/>) already has this date.</summary>
	Task<bool> DateExistsAsync(DateOnly date, int excludeId);

	/// <summary>Creates a holiday; returns the new Id. Caller checks DateExistsAsync first.</summary>
	Task<int> CreateAsync(HolidayItem item);

	/// <summary>Updates date and name. Caller checks DateExistsAsync first.</summary>
	Task UpdateAsync(HolidayItem item);

	/// <summary>Deletes a holiday (hard delete: no table references holidays).</summary>
	Task DeleteAsync(int id);

	/// <summary>
	/// Adds the suggested holidays of a year (<see cref="HolidayCalendar.Suggest"/>) whose dates
	/// are not in the list yet; existing rows are never changed. Returns how many were added.
	/// </summary>
	Task<int> AddSuggestedAsync(int year);
}
