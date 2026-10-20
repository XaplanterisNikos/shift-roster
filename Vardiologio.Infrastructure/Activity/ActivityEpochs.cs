using Vardiologio.Domain.Entities;
using Vardiologio.Infrastructure.Persistence;

namespace Vardiologio.Infrastructure.Activity;

/// <summary>Finds the current epoch of the activity log (see <see cref="DatabaseEpoch"/>).</summary>
internal static class ActivityEpochs
{
	/// <summary>Name of the epoch of a database that was never imported over.</summary>
	public const string InitialName = "Αρχική βάση";

	/// <summary>
	/// The current epoch: the row with the highest key. When the table is empty, a first epoch is
	/// added to the context (not saved here; it is saved with whatever the caller saves).
	/// </summary>
	public static DatabaseEpoch Current(AppDbContext db)
	{
		// An epoch added earlier in this context but not saved yet counts as well.
		var epoch = db.DatabaseEpochs.Local.OrderByDescending(e => e.Id).FirstOrDefault()
			?? db.DatabaseEpochs.OrderByDescending(e => e.Id).FirstOrDefault();
		if (epoch is not null) return epoch;

		epoch = new DatabaseEpoch { Name = InitialName, StartedAt = DateTime.Now };
		db.DatabaseEpochs.Add(epoch);
		return epoch;
	}
}
