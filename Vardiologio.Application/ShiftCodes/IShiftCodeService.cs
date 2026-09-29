using System.ComponentModel.DataAnnotations;
using Vardiologio.Domain.Enums;

namespace Vardiologio.Application.ShiftCodes;

/// <summary>Row shown in the settings screen's shift-code list.</summary>
public record ShiftCodeListItem(
	int Id,
	string Code,
	string Description,
	TimeOnly? StartTime,
	TimeOnly? EndTime,
	ShiftSegment? Segment,
	bool IsActive);

/// <summary>
/// One cell of a code's hour split: on this kind of day the code yields these hours in this
/// pay category. Mutable because the settings form binds to it.
/// </summary>
public class ShiftCodeHoursItem
{
	/// <summary>Kind of day (exactly one flag).</summary>
	public DayType DayType { get; set; }

	/// <summary>Pay category the hours count in.</summary>
	public HourCategory Category { get; set; }

	/// <summary>Number of hours (&gt; 0; zero cells are simply not stored).</summary>
	public decimal Hours { get; set; }
}

/// <summary>Full editable state of one shift code, including its allowed days and hour split.</summary>
public class ShiftCodeDetail
{
	public int Id { get; set; }

	/// <summary>Set only on create; ignored by UpdateAsync (the field is not editable).</summary>
	// Required: an empty/whitespace code would otherwise reach .Trim() as null and crash SaveAsync.
	[Required(ErrorMessage = "Ο κωδικός είναι υποχρεωτικός.")]
	// Mirrors HasMaxLength(10) in AppDbContext — SQLite does not enforce it, so the form must.
	[StringLength(10, ErrorMessage = "Ο κωδικός μπορεί να έχει έως 10 χαρακτήρες.")]
	public string Code { get; set; } = null!;

	// Required: Create/UpdateAsync call Description.Trim(), and the DB column is NOT NULL.
	[Required(ErrorMessage = "Η περιγραφή είναι υποχρεωτική.")]
	// Mirrors HasMaxLength(100) in AppDbContext — SQLite does not enforce it, so the form must.
	[StringLength(100, ErrorMessage = "Η περιγραφή μπορεί να έχει έως 100 χαρακτήρες.")]
	public string Description { get; set; } = null!;
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public ShiftSegment? Segment { get; set; }

	/// <summary>Kinds of day the code may be entered on; <see cref="DayType.None"/> for status codes.</summary>
	public DayType AllowedDays { get; set; }

	/// <summary>The hour split (only non-zero cells). Empty for status codes.</summary>
	public List<ShiftCodeHoursItem> Hours { get; set; } = new();

	public bool IsActive { get; set; }
}

/// <summary>CRUD (with soft-delete) for shift/status codes, their allowed days and hour split.</summary>
public interface IShiftCodeService
{
	/// <summary>All codes for the settings screen. includeInactive=false hides soft-deleted ones.</summary>
	Task<IReadOnlyList<ShiftCodeListItem>> GetAllAsync(bool includeInactive);

	/// <summary>One code for editing (works for inactive too).</summary>
	Task<ShiftCodeDetail?> GetAsync(int id);

	/// <summary>True if this Code string is already taken, active or inactive (Code is never reused).</summary>
	Task<bool> CodeExistsAsync(string code);

	/// <summary>
	/// True if at least one ShiftDay references this code. Call this before UpdateAsync when
	/// the times, Segment, allowed days or hour split changed, to decide whether to warn the
	/// user that the change will retroactively affect existing roster entries and reports.
	/// </summary>
	Task<bool> HasShiftDayReferencesAsync(int id);

	/// <summary>Creates a new code (with its allowed days and hour split); returns the new Id.</summary>
	Task<int> CreateAsync(ShiftCodeDetail detail);

	/// <summary>
	/// Updates Description, times, Segment, allowed days and the hour split (the code's split rows
	/// are replaced by <see cref="ShiftCodeDetail.Hours"/>). Code is never changed here.
	/// Caller is responsible for having warned the user via HasShiftDayReferencesAsync beforehand,
	/// if applicable — this method does not re-check or block on it.
	/// </summary>
	Task UpdateAsync(ShiftCodeDetail detail);

	/// <summary>Soft-delete (isActive=false) or restore (true). Never touches the hour split.</summary>
	Task SetActiveAsync(int id, bool isActive);
}