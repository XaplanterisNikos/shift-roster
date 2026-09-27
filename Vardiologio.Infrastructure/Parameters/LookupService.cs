using Microsoft.EntityFrameworkCore;
using Vardiologio.Application.Parameters;
using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Parameters;

/// <summary>EF Core implementation of <see cref="ILookupService"/>.</summary>
/// <remarks>
/// The three lookup tables have no global IsActive query filter (see AppDbContext),
/// so "active only" is always an explicit Where(x => x.IsActive) here.
/// </remarks>
public class LookupService : ILookupService
{
	private readonly IDbContextFactory<AppDbContext> _factory;
	public LookupService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

	/// <inheritdoc/>
	public async Task<IReadOnlyList<LookupItem>> GetAllAsync(LookupKind kind, bool includeInactive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var query = Query(db, kind);
		if (!includeInactive) query = query.Where(x => x.IsActive);

		return await query.OrderBy(x => x.Name).ToListAsync();
	}

	/// <inheritdoc/>
	public async Task<bool> NameExistsAsync(LookupKind kind, string name, int excludeId)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Active AND inactive rows: the unique index on Name covers both.
		return await Query(db, kind).AnyAsync(x => x.Name == name && x.Id != excludeId);
	}

	/// <inheritdoc/>
	public async Task<bool> EmploymentTypeCodeExistsAsync(string code, int excludeId)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Active AND inactive rows: the unique index on Code covers both.
		return await db.EmploymentTypes.AnyAsync(t => t.Code == code && t.Id != excludeId);
	}

	/// <inheritdoc/>
	public async Task<int> CountActiveEmployeesAsync(LookupKind kind, int id)
	{
		await using var db = await _factory.CreateDbContextAsync();

		// Employee DOES have the global IsActive filter, so these counts are active employees only.
		return kind switch
		{
			LookupKind.Speciality     => await db.Employees.CountAsync(e => e.SpecialityId == id),
			LookupKind.EmploymentType => await db.Employees.CountAsync(e => e.EmploymentTypeId == id),
			LookupKind.WorkPosition   => await db.Employees.CountAsync(e => e.WorkPositionId == id),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
		};
	}

	/// <inheritdoc/>
	public async Task<int> CreateAsync(LookupKind kind, LookupItem item)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var name = item.Name.Trim();

		switch (kind)
		{
			case LookupKind.Speciality:
				var speciality = new Speciality { Name = name, IsActive = true };
				db.Specialities.Add(speciality);
				await db.SaveChangesAsync();
				return speciality.Id;

			case LookupKind.EmploymentType:
				var type = new EmploymentType { Code = RequireCode(item), Name = name, IsActive = true };
				db.EmploymentTypes.Add(type);
				await db.SaveChangesAsync();
				return type.Id;

			case LookupKind.WorkPosition:
				var position = new WorkPosition { Name = name, IsActive = true };
				db.WorkPositions.Add(position);
				await db.SaveChangesAsync();
				return position.Id;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
		}
	}

	/// <inheritdoc/>
	public async Task UpdateAsync(LookupKind kind, LookupItem item)
	{
		await using var db = await _factory.CreateDbContextAsync();

		var name = item.Name.Trim();

		switch (kind)
		{
			case LookupKind.Speciality:
				var speciality = await db.Specialities.FirstOrDefaultAsync(x => x.Id == item.Id)
					?? throw new InvalidOperationException($"Speciality {item.Id} not found.");
				speciality.Name = name;
				break;

			case LookupKind.EmploymentType:
				var type = await db.EmploymentTypes.FirstOrDefaultAsync(x => x.Id == item.Id)
					?? throw new InvalidOperationException($"EmploymentType {item.Id} not found.");
				type.Code = RequireCode(item);   // Code is editable (display label only)
				type.Name = name;
				break;

			case LookupKind.WorkPosition:
				var position = await db.WorkPositions.FirstOrDefaultAsync(x => x.Id == item.Id)
					?? throw new InvalidOperationException($"WorkPosition {item.Id} not found.");
				position.Name = name;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
		}

		// Note: IsActive is changed only via SetActiveAsync, not here.
		await db.SaveChangesAsync();
	}

	/// <inheritdoc/>
	public async Task SetActiveAsync(LookupKind kind, int id, bool isActive)
	{
		await using var db = await _factory.CreateDbContextAsync();

		switch (kind)
		{
			case LookupKind.Speciality:
				var speciality = await db.Specialities.FirstOrDefaultAsync(x => x.Id == id)
					?? throw new InvalidOperationException($"Speciality {id} not found.");
				speciality.IsActive = isActive;   // false = soft-delete, true = restore
				break;

			case LookupKind.EmploymentType:
				var type = await db.EmploymentTypes.FirstOrDefaultAsync(x => x.Id == id)
					?? throw new InvalidOperationException($"EmploymentType {id} not found.");
				type.IsActive = isActive;
				break;

			case LookupKind.WorkPosition:
				var position = await db.WorkPositions.FirstOrDefaultAsync(x => x.Id == id)
					?? throw new InvalidOperationException($"WorkPosition {id} not found.");
				position.IsActive = isActive;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
		}

		await db.SaveChangesAsync();
	}

	/// <summary>
	/// Projects one lookup table to <see cref="LookupItem"/>. EF Core can compose further
	/// Where/OrderBy on the projected members, so every read goes through this one place.
	/// </summary>
	private static IQueryable<LookupItem> Query(AppDbContext db, LookupKind kind) => kind switch
	{
		LookupKind.Speciality => db.Specialities.Select(x => new LookupItem
		{
			Id = x.Id, Code = null, Name = x.Name, IsActive = x.IsActive
		}),
		LookupKind.EmploymentType => db.EmploymentTypes.Select(x => new LookupItem
		{
			Id = x.Id, Code = x.Code, Name = x.Name, IsActive = x.IsActive
		}),
		LookupKind.WorkPosition => db.WorkPositions.Select(x => new LookupItem
		{
			Id = x.Id, Code = null, Name = x.Name, IsActive = x.IsActive
		}),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	/// <summary>
	/// Trimmed Code for an employment type. The UI validates it first (check-then-act);
	/// reaching here without one is a programming error, not a user error.
	/// </summary>
	private static string RequireCode(LookupItem item) =>
		string.IsNullOrWhiteSpace(item.Code)
			? throw new ArgumentException("An employment type requires a Code.", nameof(item))
			: item.Code.Trim();
}
