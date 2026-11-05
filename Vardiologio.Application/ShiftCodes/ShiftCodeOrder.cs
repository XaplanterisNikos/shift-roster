namespace Vardiologio.Application.ShiftCodes;

/// <summary>
/// Pure rules of the user's own order of the shift codes, shared by the list of the shift
/// parameters page and the box of codes of the shift entry page (see
/// <see cref="IShiftCodeOrderStore"/>). A code is identified by its text, which never changes
/// after the code is created.
/// </summary>
public static class ShiftCodeOrder
{
	/// <summary>
	/// Puts the codes in the saved order. Codes that are not in the saved order (created
	/// later) follow at the end, in their default order; saved codes that no longer exist are
	/// ignored.
	/// </summary>
	/// <param name="items">The codes in their default order.</param>
	/// <param name="saved">The saved order; empty for the default order.</param>
	public static List<ShiftCodeListItem> Apply(IReadOnlyList<ShiftCodeListItem> items, IReadOnlyList<string> saved) =>
		Apply(items, saved, item => item.Code);

	/// <summary>
	/// Puts any kind of code item in the saved order, by the code text that
	/// <paramref name="codeOf"/> reads from it. Same rules as the list overload: unsaved codes
	/// follow at the end in their default order; saved codes that are not among the items are ignored.
	/// </summary>
	/// <typeparam name="T">The item type of the page (list row, tile of the box of codes).</typeparam>
	/// <param name="items">The items in their default order.</param>
	/// <param name="saved">The saved order; empty for the default order.</param>
	/// <param name="codeOf">Reads the code text of an item.</param>
	public static List<T> Apply<T>(IReadOnlyList<T> items, IReadOnlyList<string> saved, Func<T, string> codeOf)
	{
		var position = new Dictionary<string, int>(StringComparer.Ordinal);
		for (var i = 0; i < saved.Count; i++)
			position.TryAdd(saved[i], i);

		return items
			.Select((item, index) => (item, index))
			.OrderBy(x => position.TryGetValue(codeOf(x.item), out var p) ? p : saved.Count + x.index)
			.Select(x => x.item)
			.ToList();
	}

	/// <summary>
	/// The order to save after the user rearranged the visible codes. Codes that are saved but
	/// not visible (inactive codes while they are hidden, or not offered in the box of codes)
	/// keep their place: each one stays right after the code that came before it in the saved order.
	/// </summary>
	/// <param name="saved">The order saved so far.</param>
	/// <param name="visible">The visible codes in their new order.</param>
	public static List<string> Merge(IReadOnlyList<string> saved, IReadOnlyList<string> visible)
	{
		var result = visible.Distinct(StringComparer.Ordinal).ToList();
		var placed = new HashSet<string>(result, StringComparer.Ordinal);

		string? previous = null;
		foreach (var code in saved)
		{
			if (!placed.Contains(code))
			{
				var at = previous is null ? 0 : result.IndexOf(previous) + 1;
				result.Insert(at, code);
				placed.Add(code);
			}
			previous = code;
		}
		return result;
	}

	/// <summary>
	/// Moves one item of a list to a new position.
	/// </summary>
	/// <param name="items">The list, changed in place.</param>
	/// <param name="item">The item to move.</param>
	/// <param name="newIndex">Its position after the move; ignored when negative.</param>
	/// <returns>True when the list changed.</returns>
	public static bool Move<T>(List<T> items, T item, int newIndex)
	{
		var oldIndex = items.IndexOf(item);
		if (oldIndex < 0 || newIndex < 0 || newIndex == oldIndex) return false;

		items.RemoveAt(oldIndex);
		items.Insert(Math.Min(newIndex, items.Count), item);
		return true;
	}
}
