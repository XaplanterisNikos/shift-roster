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
	decimal? ExtraHours,
	bool IsActive);

/// <summary>Full editable state of one shift code, including its 1:1 extra-hours value.</summary>
public class ShiftCodeDetail
{
	public int Id { get; set; }

	/// <summary>Set only on create; ignored by UpdateAsync (the field is not editable).</summary>
	public string Code { get; set; } = null!;

	public string Description { get; set; } = null!;
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public ShiftSegment? Segment { get; set; }

	/// <summary>Null means "no extra-hours row for this code" (most status codes).</summary>
	public decimal? ExtraHours { get; set; }

	public bool IsActive { get; set; }
}

/// <summary>CRUD (with soft-delete) for shift/status codes and their optional extra-hours value.</summary>
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
	/// StartTime, EndTime, or Segment changed, to decide whether to warn the user that the
	/// change will retroactively affect existing roster entries.
	/// </summary>
	Task<bool> HasShiftDayReferencesAsync(int id);

	/// <summary>Creates a new code (plus its extra-hours row, if provided); returns the new Id.</summary>
	Task<int> CreateAsync(ShiftCodeDetail detail);

	/// <summary>
	/// Updates Description, StartTime, EndTime, Segment and ExtraHours. Code is never changed here.
	/// Caller is responsible for having warned the user via HasShiftDayReferencesAsync beforehand,
	/// if applicable — this method does not re-check or block on it.
	/// </summary>
	Task UpdateAsync(ShiftCodeDetail detail);

	/// <summary>Soft-delete (isActive=false) or restore (true). Never touches ExtraHours.</summary>
	Task SetActiveAsync(int id, bool isActive);
}