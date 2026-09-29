using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Hours;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Hours;

/// <summary>EF Core implementation of <see cref="IHourLimitsService"/> (single-row table).</summary>
public class HourLimitsService : IHourLimitsService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public HourLimitsService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<HourLimitsDetail> GetAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		// The seeder always creates exactly one row; a missing row is a setup error.
		return await db.HourLimits
			.OrderBy(l => l.Id)
			.Select(l => new HourLimitsDetail
			{
				ToCompleteMonthly = l.ToCompleteMonthly,
				SimpleMonthly = l.SimpleMonthly,
				NightMonthly = l.NightMonthly,
				SundayMonthly = l.SundayMonthly,
				HolidayPerHoliday = l.HolidayPerHoliday,
				ToCompletePlusHolidayMonthly = l.ToCompletePlusHolidayMonthly
			})
			.FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(HourLimitsDetail d)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.HourLimits.OrderBy(l => l.Id).FirstOrDefaultAsync()
			?? throw new InvalidOperationException("HourLimits row not found.");

		entity.ToCompleteMonthly = d.ToCompleteMonthly;
		entity.SimpleMonthly = d.SimpleMonthly;
		entity.NightMonthly = d.NightMonthly;
		entity.SundayMonthly = d.SundayMonthly;
		entity.HolidayPerHoliday = d.HolidayPerHoliday;
		entity.ToCompletePlusHolidayMonthly = d.ToCompletePlusHolidayMonthly;
		await db.SaveChangesAsync();
	}
}
