namespace Vardiologio.Application.ShiftCodes;

/// <summary>
/// The order in which the shift codes are listed on the shift parameters page, as the user
/// arranged it by dragging. Display only: it is kept on this computer, outside the database,
/// and changes nothing in how the codes work or how the reports list them.
/// </summary>
public interface IShiftCodeOrderStore
{
	/// <summary>The codes in the saved order; empty when the user has not arranged them.</summary>
	IReadOnlyList<string> Load();

	/// <summary>Saves the codes in the given order, replacing the saved order.</summary>
	/// <param name="codes">Every code the user arranged, first to last.</param>
	void Save(IReadOnlyList<string> codes);

	/// <summary>Removes the saved order, so the page lists the codes in their default order again.</summary>
	void Reset();
}
