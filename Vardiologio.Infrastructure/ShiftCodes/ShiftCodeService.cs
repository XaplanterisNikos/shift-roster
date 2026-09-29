using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.ShiftCodes;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.ShiftCodes;

/// <summary>EF Core implementation of <see cref="IShiftCodeService"/>.</summary>
public class ShiftCodeService : IShiftCodeService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ShiftCodeService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<ShiftCodeListItem>> GetAllAsync(bool includeInactive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// No global filter on ShiftCode (see AppDbContext), so hide inactive rows explicitly.
		var query = db.ShiftCodes.AsQueryable();
		if (!includeInactive) query = query.Where(c => c.IsActive);

		return await query
			.OrderBy(c => c.Id)
			.Select(c => new ShiftCodeListItem(
				c.Id,
				c.Code,
				c.Description,
				c.StartTime,
				c.EndTime,
				c.Segment,
				c.IsActive))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<ShiftCodeDetail?> GetAsync(int id)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// No IsActive condition: an inactive code can still be opened (e.g. to view/restore).
		var detail = await db.ShiftCodes
			.Where(c => c.Id == id)
			.Select(c => new ShiftCodeDetail
			{
				Id = c.Id,
				Code = c.Code,
				Description = c.Description,
				StartTime = c.StartTime,
				EndTime = c.EndTime,
				Segment = c.Segment,
				AllowedDays = c.AllowedDays,
				IsActive = c.IsActive
			})
			.FirstOrDefaultAsync();

		if (detail is null) return null;

		// The split in a second query: a handful of rows, keeps the projection above simple.
		detail.Hours = await db.ShiftCodeHours
			.Where(h => h.ShiftCodeId == id)
			.Select(h => new ShiftCodeHoursItem { DayType = h.DayType, Category = h.Category, Hours = h.Hours })
			.ToListAsync();

		return detail;
	}

	/// <inheritdoc/>
	public async Task<bool> CodeExistsAsync(string code)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Checks active AND inactive rows: a retired code is never reused.
		return await db.ShiftCodes.AnyAsync(c => c.Code == code);
	}

	/// <inheritdoc/>
	public async Task<bool> HasShiftDayReferencesAsync(int id)
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.ShiftDays.AnyAsync(sd => sd.ShiftCodeId == id);
	}

	/// <inheritdoc/>
	public async Task<int> CreateAsync(ShiftCodeDetail d)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = new ShiftCode
		{
			Code = d.Code.Trim(),
			Description = d.Description.Trim(),
			StartTime = d.StartTime,
			EndTime = d.EndTime,
			Segment = d.Segment,
			AllowedDays = d.AllowedDays,
			IsActive = true                     // new codes start active
		};
		db.ShiftCodes.Add(entity);

		// Split rows attach through the navigation, so EF fills ShiftCodeId after the insert
		// and one SaveChanges is enough.
		foreach (var h in d.Hours)
			db.ShiftCodeHours.Add(new ShiftCodeHours { ShiftCode = entity, DayType = h.DayType, Category = h.Category, Hours = h.Hours });

		await db.SaveChangesAsync();
		return entity.Id;
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(ShiftCodeDetail d)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// No IsActive condition, so an inactive code can be updated too.
		var entity = await db.ShiftCodes
			.FirstOrDefaultAsync(c => c.Id == d.Id)
			?? throw new InvalidOperationException($"ShiftCode {d.Id} not found.");

		// Code is intentionally never updated here — it is immutable after creation.
		entity.Description = d.Description.Trim();
		entity.StartTime = d.StartTime;
		entity.EndTime = d.EndTime;
		entity.Segment = d.Segment;
		entity.AllowedDays = d.AllowedDays;
		// Note: IsActive is changed only via SetActiveAsync, not here.

		// Sync the split with the form's cells in place (key = code + day + category):
		// update cells that still exist, remove cells that are gone, add new ones.
		// One SaveChanges, so the code and its split are saved together (one transaction).
		var oldRows = await db.ShiftCodeHours
			.Where(h => h.ShiftCodeId == d.Id)
			.ToDictionaryAsync(h => (h.DayType, h.Category));

		foreach (var h in d.Hours)
		{
			if (oldRows.Remove((h.DayType, h.Category), out var row))
				row.Hours = h.Hours;   // existing cell: new value
			else
				db.ShiftCodeHours.Add(new ShiftCodeHours { ShiftCodeId = d.Id, DayType = h.DayType, Category = h.Category, Hours = h.Hours });
		}

		// What is left in oldRows is no longer in the form.
		db.ShiftCodeHours.RemoveRange(oldRows.Values);

		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task SetActiveAsync(int id, bool isActive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.ShiftCodes
			.FirstOrDefaultAsync(c => c.Id == id)
			?? throw new InvalidOperationException($"ShiftCode {id} not found.");

		entity.IsActive = isActive;   // false = soft-delete, true = restore
		await db.SaveChangesAsync();
	}
}
