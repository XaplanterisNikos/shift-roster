using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Parameters;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Parameters;

/// <summary>EF Core implementation of <see cref="ISignatoryService"/>.</summary>
/// <remarks>
/// Signatories have no global IsActive query filter (like the lookups), so "active only" is an
/// explicit Where here. Employees DO have one, so the employee queries see active ones only.
/// </remarks>
public class SignatoryService : ISignatoryService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public SignatoryService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SignatoryItem>> GetAllAsync(bool includeInactive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var query = db.Signatories.AsNoTracking();
		if (!includeInactive) query = query.Where(s => s.IsActive);

		return await query
			.OrderBy(s => s.FullName)
			.Select(s => new SignatoryItem
			{
				Id = s.Id,
				Role = s.Role,
				FullName = s.FullName,
				Title = s.Title,
				IsActive = s.IsActive
			})
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<int> CreateAsync(SignatoryItem item)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = new Signatory { IsActive = true };
		Copy(item, entity);
		db.Signatories.Add(entity);
		await db.SaveChangesAsync();
		return entity.Id;
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(SignatoryItem item)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.Signatories.FirstOrDefaultAsync(s => s.Id == item.Id)
			?? throw new InvalidOperationException($"Signatory {item.Id} not found.");
		Copy(item, entity);
		// Note: IsActive is changed only via SetActiveAsync, not here.
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task SetActiveAsync(int id, bool isActive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var entity = await db.Signatories.FirstOrDefaultAsync(s => s.Id == id)
			?? throw new InvalidOperationException($"Signatory {id} not found.");
		entity.IsActive = isActive;   // false = soft-delete, true = restore
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<SignatoryEmployee>> GetEmployeesAsync()
	{
		await using var db = await _factory.CreateDbContextAsync();

		return await db.Employees
			.OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
			.Select(e => new SignatoryEmployee(e.Id, e.LastName + " " + e.FirstName, e.SignatoryId))
			.ToListAsync();
	}

	/// <inheritdoc/>
	public async Task SetEmployeesAsync(int signatoryId, IReadOnlyCollection<int> employeeIds)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// A List in the query (EF translates List.Contains to SQL IN), a set for the loop below.
		var wantedIds = employeeIds.Distinct().ToList();
		var wanted = wantedIds.ToHashSet();

		// Active employees that have this supervisor now, or should have it: only these change.
		var rows = await db.Employees
			.Where(e => e.SignatoryId == signatoryId || wantedIds.Contains(e.Id))
			.ToListAsync();

		foreach (var employee in rows)
		{
			if (wanted.Contains(employee.Id))
				employee.SignatoryId = signatoryId;   // listed: signs for them (replaces any other)
			else
				employee.SignatoryId = null;          // had it, no longer listed: left without one
		}
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task<int?> GetEmployeeSignatoryIdAsync(int employeeId)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// IgnoreQueryFilters: the sheet of an employee deactivated later may still be printed.
		return await db.Employees
			.IgnoreQueryFilters()
			.Where(e => e.Id == employeeId)
			.Select(e => e.SignatoryId)
			.FirstOrDefaultAsync();
	}

	/// <summary>Copies the editable fields (trimmed, role lines normalised to "\n") onto the entity.</summary>
	private static void Copy(SignatoryItem item, Signatory entity)
	{
		entity.Role = string.Join("\n", SignatoryItem.SplitLines(item.Role));
		entity.FullName = item.FullName.Trim();
		entity.Title = (item.Title ?? "").Trim();
	}
}
