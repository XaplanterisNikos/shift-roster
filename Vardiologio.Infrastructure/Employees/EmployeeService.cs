using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Employees;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Employees;

/// <summary>EF Core implementation of <see cref="IEmployeeService"/>.</summary>
public class EmployeeService : IEmployeeService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public EmployeeService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<LookupOption>> GetSpecialitiesAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();
		return await db.Specialities
			.OrderBy(s => s.Name)
			.Select(s => new LookupOption(s.Id, s.Name))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<EmployeeListItem>> GetBySpecialityAsync(int specialityId, bool includeInactive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Base query. When inactive should be shown, bypass the global "IsActive" filter.
		var query = db.Employees.AsQueryable();
		if (includeInactive) query = query.IgnoreQueryFilters();

		return await query
			.Where(e => e.SpecialityId == specialityId)
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new EmployeeListItem(e.Id, e.LastName + " " + e.FirstName, e.IsActive))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<EmployeeDetail?> GetAsync(int id)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// IgnoreQueryFilters so an inactive employee can still be opened (e.g. to restore).
		return await db.Employees
			.IgnoreQueryFilters()
			.Where(e => e.Id == id)
			.Select(e => new EmployeeDetail
			{
				Id = e.Id,
				LastName = e.LastName,
				FirstName = e.FirstName,
				SpecialityId = e.SpecialityId,
				EmploymentTypeId = e.EmploymentTypeId,
				WorkPositionId = e.WorkPositionId,
				IsActive = e.IsActive
			})
			.FirstOrDefaultAsync();
	}

	/// <inheritdoc/>
	public async Task<EmployeeLookups> GetLookupsAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		var specs = await db.Specialities.OrderBy(s => s.Name)
			.Select(s => new LookupOption(s.Id, s.Name)).ToListAsync();
		var types = await db.EmploymentTypes.OrderBy(t => t.Name)
			.Select(t => new LookupOption(t.Id, t.Name)).ToListAsync();
		var positions = await db.WorkPositions.OrderBy(p => p.Name)
			.Select(p => new LookupOption(p.Id, p.Name)).ToListAsync();

		return new EmployeeLookups(specs, types, positions);
	}

	/// <inheritdoc/>
	public async Task<int> CreateAsync(EmployeeDetail d)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = new Employee
		{
			LastName = d.LastName.Trim(),
			FirstName = d.FirstName.Trim(),
			SpecialityId = d.SpecialityId,
			EmploymentTypeId = d.EmploymentTypeId,
			WorkPositionId = d.WorkPositionId,
			IsActive = true                     // new employees start active
		};
		db.Employees.Add(entity);
		await db.SaveChangesAsync();
		return entity.Id;
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(EmployeeDetail d)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// IgnoreQueryFilters so we can also update an inactive employee.
		var entity = await db.Employees.IgnoreQueryFilters()
			.FirstOrDefaultAsync(e => e.Id == d.Id)
			?? throw new InvalidOperationException($"Employee {d.Id} not found.");

		entity.LastName = d.LastName.Trim();
		entity.FirstName = d.FirstName.Trim();
		entity.SpecialityId = d.SpecialityId;
		entity.EmploymentTypeId = d.EmploymentTypeId;
		entity.WorkPositionId = d.WorkPositionId;
		// Note: IsActive is changed only via SetActiveAsync, not here.

		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task SetActiveAsync(int id, bool isActive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.Employees.IgnoreQueryFilters()
			.FirstOrDefaultAsync(e => e.Id == id)
			?? throw new InvalidOperationException($"Employee {id} not found.");

		entity.IsActive = isActive;   // false = soft-delete, true = restore
		await db.SaveChangesAsync();
	}
}