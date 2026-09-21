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

		// Base query. When inactive should be shown, bypass the global "IsActive" filter.
		var query = db.ShiftCodes.AsQueryable();
		if (includeInactive) query = query.IgnoreQueryFilters();

		// Left join to ShiftExtraHours since most status codes have no extra-hours row.
		return await query
			.OrderBy(c => c.Id)
			.Select(c => new ShiftCodeListItem(
				c.Id,
				c.Code,
				c.Description,
				c.StartTime,
				c.EndTime,
				c.Segment,
				db.ShiftExtraHours
					.Where(x => x.ShiftCodeId == c.Id)
					.Select(x => (decimal?)x.Hours)
					.FirstOrDefault(),
				c.IsActive))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<ShiftCodeDetail?> GetAsync(int id)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// IgnoreQueryFilters so an inactive code can still be opened (e.g. to view/restore).
		return await db.ShiftCodes
			.IgnoreQueryFilters()
			.Where(c => c.Id == id)
			.Select(c => new ShiftCodeDetail
			{
				Id = c.Id,
				Code = c.Code,
				Description = c.Description,
				StartTime = c.StartTime,
				EndTime = c.EndTime,
				Segment = c.Segment,
				ExtraHours = db.ShiftExtraHours
					.Where(x => x.ShiftCodeId == c.Id)
					.Select(x => (decimal?)x.Hours)
					.FirstOrDefault(),
				IsActive = c.IsActive
			})
			.FirstOrDefaultAsync();
	}

	/// <inheritdoc/>
	public async Task<bool> CodeExistsAsync(string code)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// IgnoreQueryFilters: Code must be unique across active AND inactive rows —
		// a retired code is never reused (see design decision in chat).
		return await db.ShiftCodes.IgnoreQueryFilters().AnyAsync(c => c.Code == code);
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
			IsActive = true                     // new codes start active
		};
		db.ShiftCodes.Add(entity);

		// SaveChanges once here first so entity.Id is generated before we use it as the
		// ShiftExtraHours FK/PK below.
		await db.SaveChangesAsync();

		if (d.ExtraHours is decimal hours)
		{
			db.ShiftExtraHours.Add(new ShiftExtraHours
			{
				ShiftCodeId = entity.Id,
				Hours = hours
			});
			await db.SaveChangesAsync();
		}

		return entity.Id;
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(ShiftCodeDetail d)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// IgnoreQueryFilters so we can also update an inactive code.
		var entity = await db.ShiftCodes.IgnoreQueryFilters()
			.FirstOrDefaultAsync(c => c.Id == d.Id)
			?? throw new InvalidOperationException($"ShiftCode {d.Id} not found.");

		// Code is intentionally never updated here — it is immutable after creation.
		entity.Description = d.Description.Trim();
		entity.StartTime = d.StartTime;
		entity.EndTime = d.EndTime;
		entity.Segment = d.Segment;
		// Note: IsActive is changed only via SetActiveAsync, not here.

		// ExtraHours is a separate 1:1 table with no ShiftDay reference, so it is safe to
		// upsert/remove it here regardless of the ShiftDay-history warning logic above —
		// nothing else in the app depends on its value changing.
		var extraHoursRow = await db.ShiftExtraHours.FirstOrDefaultAsync(x => x.ShiftCodeId == d.Id);
		if (d.ExtraHours is decimal hours)
		{
			if (extraHoursRow is null)
				db.ShiftExtraHours.Add(new ShiftExtraHours { ShiftCodeId = d.Id, Hours = hours });
			else
				extraHoursRow.Hours = hours;
		}
		else if (extraHoursRow is not null)
		{
			db.ShiftExtraHours.Remove(extraHoursRow);
		}

		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task SetActiveAsync(int id, bool isActive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.ShiftCodes.IgnoreQueryFilters()
			.FirstOrDefaultAsync(c => c.Id == id)
			?? throw new InvalidOperationException($"ShiftCode {id} not found.");

		entity.IsActive = isActive;   // false = soft-delete, true = restore
		await db.SaveChangesAsync();
	}
}