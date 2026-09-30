using ClosedXML.Excel;
using Vardiologio.Application.Reports;

namespace Vardiologio.Infrastructure.Reports;

/// <summary>
/// Attendance report ("ΠΙΝΑΚΑΣ ΠΑΡΟΥΣΙΩΝ ΠΡΟΣΩΠΙΚΟΥ") — one file per report (staff or directorate),
/// the printed pages stacked vertically with a page break after each: emblem + letterhead + titles,
/// the table, and the signature block on every page. Reuses Put/Merge of the Σ.Ω. part.
/// </summary>
public partial class ClosedXmlReportRenderer
{
	// Column layout shared by every page: name, speciality, then up to 16 day slots
	// (days 1–15 in the first fortnight, 16–end in the second).
	private const int AttName = 1, AttSpec = 2, AttDay0 = 3, AttDaySlots = 16;
	private const int AttLastCol = AttDay0 + AttDaySlots - 1;

	/// <inheritdoc/>
	public byte[] Render(AttendanceReportModel m)
	{
		using var wb = new XLWorkbook();
		var ws = wb.AddWorksheet(AttendanceSheetTexts.SheetName(m.Sheet));

		// Pages one under the other; a manual page break closes each one.
		var row = 1;
		var pageNo = 0;
		foreach (var page in m.Pages())
		{
			if (pageNo > 0) ws.PageSetup.AddHorizontalPageBreak(row - 1);
			row = AttendancePage(ws, m, page.From, page.To, page.Table, page.Rows, row, ++pageNo);
		}
		var lastRow = row - 1;

		// Column widths chosen so a page is one A4 landscape wide.
		ws.Column(AttName).Width = 26;
		ws.Column(AttSpec).Width = 30;   // longest specialities ("ΥΕ Προσωπικού Καθαριότητας Εξωτ. Χώρων")
		for (var c = AttDay0; c <= AttLastCol; c++) ws.Column(c).Width = 7;

		ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
		ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
		ws.PageSetup.PagesWide = 1;
		ws.PageSetup.CenterHorizontally = true;
		ws.PageSetup.PrintAreas.Add(1, 1, lastRow, AttLastCol);
		ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3;
		ws.PageSetup.Margins.Top = 0.3; ws.PageSetup.Margins.Bottom = 0.3;
		ws.PageSetup.Margins.Header = 0; ws.PageSetup.Margins.Footer = 0;

		using var ms = new MemoryStream();
		wb.SaveAs(ms);
		return ms.ToArray();
	}

	/// <summary>
	/// Writes one printed page starting at <paramref name="top"/> and returns the first row after it.
	/// </summary>
	private static int AttendancePage(IXLWorksheet ws, AttendanceReportModel m,
		DateOnly from, DateOnly to, int table, AttendanceReportRow[] rows, int top, int pageNo)
	{
		var r = top;
		var titleFrom = AttDay0 + 2;       // centre titles span the middle day columns
		var titleTo = AttLastCol - 2;
		var formal = AttendanceSheetTexts.IsFormal(m.Sheet);

		// Directorate only: addressee line above everything, bold and centred.
		var addressee = AttendanceSheetTexts.Addressee(m.Sheet);
		if (addressee is not null)
		{
			Merge(ws, r, titleFrom, r, titleTo, addressee, 11, true, XLAlignmentHorizontalValues.Center, null);
			ws.Row(r).Height = 16;
			r++;
		}

		// Emblem: three rows high, top left, above the letterhead (as on the original sheet).
		for (var i = 0; i < 3; i++) ws.Row(r + i).Height = 15;
		using var emblem = new MemoryStream(AttendanceSheetTexts.Emblem);   // ClosedXML copies the image
		ws.AddPicture(emblem, $"Emblem{pageNo}")
			.MoveTo(ws.Cell(r, AttName), 6, 2)
			.WithSize(58, 56);
		r += 3;

		// Letterhead (left) and centre titles + period, on the same rows.
		var lh = AttendanceSheetTexts.Letterhead;
		for (var i = 0; i < lh.Length; i++)
		{
			Merge(ws, r + i, AttName, r + i, AttSpec, lh[i], 9, i < AttendanceSheetTexts.LetterheadBoldLines,
				XLAlignmentHorizontalValues.Left, null);
			ws.Row(r + i).Height = 12.75;
		}
		var titles = AttendanceSheetTexts.Titles(m.Sheet);
		for (var i = 0; i < titles.Length; i++)
			Merge(ws, r + i, titleFrom, r + i, titleTo, titles[i], 11, true, XLAlignmentHorizontalValues.Center, null);
		var periodRow = r + titles.Length + (formal ? 1 : 0);   // formal: a blank line before the period
		if (formal)
			AttendanceFormalPeriod(ws, periodRow, from, to);
		else
			Merge(ws, periodRow, titleFrom, periodRow, titleTo,
				AttendanceSheetTexts.Period(from, to), 10, false, XLAlignmentHorizontalValues.Center, null);
		r += lh.Length;

		// Spacer + table caption ("ΠΙΝΑΚΑΣ 1").
		ws.Row(r).Height = 6;
		r++;
		Put(ws, r, AttName, AttendanceSheetTexts.TableCaption(table), 10, true, null)
			.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
		ws.Row(r).Height = 15;
		r++;

		// Table header: name / speciality over two rows, then day number + weekday per day.
		var days = to.Day - from.Day + 1;
		var lastDayCol = AttDay0 + days - 1;
		var headerTop = r;
		Merge(ws, r, AttName, r + 1, AttName, "ΟΝΟΜΑΤΕΠΩΝΥΜΟ", 10, true, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, r, AttSpec, r + 1, AttSpec, "ΕΙΔΙΚΟΤΗΤΑ", 10, true, XLAlignmentHorizontalValues.Center, null);
		for (var i = 0; i < days; i++)
		{
			var date = from.AddDays(i);
			Put(ws, r, AttDay0 + i, date.Day, 9, true, null);
			Put(ws, r + 1, AttDay0 + i, AttendanceSheetTexts.DayName(date), 6, false, null);
		}
		ws.Row(r).Height = 15;
		ws.Row(r + 1).Height = 12;
		r += 2;

		// One row per employee on this page.
		foreach (var row in rows)
		{
			Put(ws, r, AttName, row.FullName, 9, false, null)
				.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
			Put(ws, r, AttSpec, row.Speciality, 8, false, null)
				.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
			for (var i = 0; i < days; i++)
				Put(ws, r, AttDay0 + i, row.Mark(from.Day + i), 10, false, null);
			ws.Row(r).Height = 16;
			r++;
		}

		// Borders: medium frame, thin grid (only the days of this fortnight).
		var grid = ws.Range(headerTop, AttName, Math.Max(r - 1, headerTop + 1), lastDayCol);
		grid.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
		grid.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

		// Signatures: role lines, blank space to sign, then name / title lines.
		ws.Row(r).Height = 8;
		r++;
		var signatures = AttendanceSheetTexts.Signatures(m.Sheet);
		var roleRows = signatures.Max(s => s.Role.Length);
		var nameRows = signatures.Max(s => s.Name.Length);
		var gapRows = formal ? 5 : 2;   // blank rows left for the handwritten signatures
		for (var k = 0; k < signatures.Length; k++)
		{
			var (c1, c2) = SignatureColumns(k, signatures.Length);
			for (var i = 0; i < signatures[k].Role.Length; i++)
				Merge(ws, r + i, c1, r + i, c2, signatures[k].Role[i], formal ? 11 : 10, formal, XLAlignmentHorizontalValues.Center, null);
			for (var i = 0; i < signatures[k].Name.Length; i++)
				Merge(ws, r + roleRows + gapRows + i, c1, r + roleRows + gapRows + i, c2,
					signatures[k].Name[i], formal ? 11 : 10, formal, XLAlignmentHorizontalValues.Center, null);
		}
		for (var i = 0; i < roleRows + gapRows + nameRows; i++)
			ws.Row(r + i).Height = i >= roleRows && i < roleRows + gapRows ? 15 : 13.5;
		r += roleRows + gapRows + nameRows;

		return r;
	}

	/// <summary>
	/// Directorate period line, laid out as on its sheet: "Από" [date] "έως" [date],
	/// the dates bold with a line under them.
	/// </summary>
	private static void AttendanceFormalPeriod(IXLWorksheet ws, int row, DateOnly from, DateOnly to)
	{
		const int cFrom = AttDay0 + 4, cDate1 = AttDay0 + 5, cTo = AttDay0 + 8, cDate2 = AttDay0 + 10;

		Put(ws, row, cFrom, "Από", 10, false, null).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
		Merge(ws, row, cDate1, row, cDate1 + 2, AttendanceSheetTexts.Date(from), 10, true, XLAlignmentHorizontalValues.Center, null);
		ws.Range(row, cDate1, row, cDate1 + 2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
		Merge(ws, row, cTo, row, cTo + 1, "έως", 10, false, XLAlignmentHorizontalValues.Center, null);
		Merge(ws, row, cDate2, row, cDate2 + 2, AttendanceSheetTexts.Date(to), 10, true, XLAlignmentHorizontalValues.Center, null);
		ws.Range(row, cDate2, row, cDate2 + 2).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
	}

	/// <summary>
	/// Columns of the k-th signature block: with two blocks the first sits under name/speciality
	/// and the second at the right; a single block sits at the right.
	/// </summary>
	private static (int From, int To) SignatureColumns(int k, int count) =>
		count > 1 && k == 0 ? (AttName, AttSpec) : (AttDay0 + 8, AttLastCol);
}
