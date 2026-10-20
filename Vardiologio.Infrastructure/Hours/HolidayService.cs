using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Hours;

/// <summary>EF Core implementation of <see cref="IHolidayService"/>.</summary>
public class HolidayService : IHolidayService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public HolidayService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<HolidayItem>> GetByYearAsync(int year)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Range on the date (not Date.Year) so the query stays a simple comparison in SQLite.
		var first = new DateOnly(year, 1, 1);
		var last = new DateOnly(year, 12, 31);

		return await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.OrderBy(h => h.Date)
			.Select(h => new HolidayItem { Id = h.Id, Date = h.Date, Name = h.Name })
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<bool> DateExistsAsync(DateOnly date, int excludeId)
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.Holidays.AnyAsync(h => h.Date == date && h.Id != excludeId);
	}

	/// <inheritdoc/>
	public async Task<int> CreateAsync(HolidayItem item)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = new Holiday { Date = item.Date, Name = item.Name.Trim() };
		db.Holidays.Add(entity);
		await db.SaveChangesAsync();
		return entity.Id;
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(HolidayItem item)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.Holidays.FirstOrDefaultAsync(h => h.Id == item.Id)
			?? throw new InvalidOperationException($"Holiday {item.Id} not found.");

		entity.Date = item.Date;
		entity.Name = item.Name.Trim();
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task DeleteAsync(int id)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.Holidays.FirstOrDefaultAsync(h => h.Id == id)
			?? throw new InvalidOperationException($"Holiday {id} not found.");

		// Hard delete is fine: no table has an FK to Holiday.
		db.Holidays.Remove(entity);
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task<int> AddSuggestedAsync(int year)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var first = new DateOnly(year, 1, 1);
		var last = new DateOnly(year, 12, 31);

		// Dates already in the list for that year: never duplicated, never overwritten.
		var existing = (await db.Holidays
			.Where(h => h.Date >= first && h.Date <= last)
			.Select(h => h.Date)
			.ToListAsync())
			.ToHashSet();

		var toAdd = HolidayCalendar.Suggest(year)
			.Where(s => !existing.Contains(s.Date))
			.Select(s => new Holiday { Date = s.Date, Name = s.Name })
			.ToList();

		db.Holidays.AddRange(toAdd);
		await db.SaveChangesAsync();
		return toAdd.Count;
	}
}
