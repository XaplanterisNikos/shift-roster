using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.ShiftEntry;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.ShiftEntry;

/// <summary>EF Core implementation of <see cref="IShiftEntryService"/>.</summary>
public class ShiftEntryService : IShiftEntryService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public ShiftEntryService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<EmployeeListItem>> GetEmployeesAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new EmployeeListItem(
				e.Id, e.LastName + " " + e.FirstName + " — " + e.Speciality.Name))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<ShiftCodeOption>> GetShiftCodesAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.ShiftCodes
			.OrderBy(c => c.Id)
			.Select(c => new ShiftCodeOption(c.Id, c.Code + " — " + c.Description))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<MonthEntry> LoadMonthAsync(int employeeId, int year, int month)
	{
		var first = new DateOnly(year, month, 1);
		var count = DateTime.DaysInMonth(year, month);
		var last = new DateOnly(year, month, count);

		await using var db = await _factory.CreateDbContextAsync();
		// Existing entries of this month, keyed by date, so we can pre-fill the grid in one query.
		var existing = await db.ShiftDays
			.Where(s => s.EmployeeId == employeeId && s.Date >= first && s.Date <= last)
			.ToDictionaryAsync(s => s.Date, s => s.ShiftCodeId);

		var entry = new MonthEntry { EmployeeId = employeeId, Year = year, Month = month };
		for (int d = 1; d <= count; d++)
		{
			var date = new DateOnly(year, month, d);
			entry.Days.Add(new DayEntry
			{
				Date = date,
				ShiftCodeId = existing.TryGetValue(date, out var id) ? id : null
			});
		}
		return entry;
	}

	/// <inheritdoc/>
	public async Task SaveMonthAsync(MonthEntry entry)
	{
		var first = new DateOnly(entry.Year, entry.Month, 1);
		var count = DateTime.DaysInMonth(entry.Year, entry.Month);
		var last = new DateOnly(entry.Year, entry.Month, count);

		await using var db = await _factory.CreateDbContextAsync();
		var rows = await db.ShiftDays
			.Where(s => s.EmployeeId == entry.EmployeeId && s.Date >= first && s.Date <= last)
			.ToListAsync();
		var byDate = rows.ToDictionary(s => s.Date);

		foreach (var day in entry.Days)
		{
			byDate.TryGetValue(day.Date, out var row);

			if (day.ShiftCodeId is null)
			{
				if (row is not null) db.ShiftDays.Remove(row);            // day cleared → delete
			}
			else if (row is null)
			{
				db.ShiftDays.Add(new ShiftDay                          // new entry
				{
					EmployeeId = entry.EmployeeId,
					Date = day.Date,
					ShiftCodeId = day.ShiftCodeId.Value
				});
			}
			else if (row.ShiftCodeId != day.ShiftCodeId.Value)
			{
				row.ShiftCodeId = day.ShiftCodeId.Value;                  // changed → update
			}
		}
		await db.SaveChangesAsync();
	}
}