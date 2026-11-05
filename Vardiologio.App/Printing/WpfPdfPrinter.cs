using System.IO;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vardiologio.Application.Printing;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace Vardiologio.App.Printing;

/// <summary>
/// Prints PDF files from the desktop window. Windows has no "print this PDF" of its own, so each
/// page is drawn to an image by the PDF engine of Windows (Windows.Data.Pdf, 300 dpi) and the
/// images are printed as a WPF document through the print window the user answered once.
/// Lives in the App project (not Infrastructure) because it needs WPF and the Windows SDK.
/// </summary>
public sealed class WpfPdfPrinter : IPdfPrinter
{
	/// <inheritdoc/>
	public IPrintTarget? ChoosePrinter() =>
		System.Windows.Application.Current.Dispatcher.Invoke(() =>
		{
			var dialog = new PrintDialog { UserPageRangeEnabled = false };
			return dialog.ShowDialog() == true ? new Target(dialog) : null;
		});

	/// <summary>One page of a PDF drawn to an image, with its size in WPF units (1/96 inch).</summary>
	/// <param name="Image">The page, frozen so it can be used on the UI thread.</param>
	/// <param name="Width">Width of the page.</param>
	/// <param name="Height">Height of the page.</param>
	private sealed record RenderedPage(BitmapSource Image, double Width, double Height);

	/// <summary>The printer and settings (copies) of one print window, used for every job of a batch.</summary>
	/// <param name="dialog">The answered print window.</param>
	private sealed class Target(PrintDialog dialog) : IPrintTarget
	{
		/// <summary>Resolution the pages are drawn at.</summary>
		private const double PrintDpi = 300;

		/// <summary>WPF and the PDF engine measure pages in 1/96 inch.</summary>
		private const double UnitDpi = 96;

		/// <inheritdoc/>
		public async Task PrintAsync(byte[] pdf, string jobName)
		{
			var pages = await RenderPagesAsync(pdf);
			if (pages.Count == 0) return;

			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				var document = new FixedDocument();
				foreach (var page in pages)
				{
					var fixedPage = new FixedPage { Width = page.Width, Height = page.Height };
					fixedPage.Children.Add(new Image
					{
						Source = page.Image,
						Width = page.Width,
						Height = page.Height,
						Stretch = Stretch.Fill
					});
					document.Pages.Add(new PageContent { Child = fixedPage });
				}

				var first = pages[0];
				document.DocumentPaginator.PageSize = new System.Windows.Size(first.Width, first.Height);
				dialog.PrintTicket.PageOrientation = first.Width > first.Height
					? PageOrientation.Landscape
					: PageOrientation.Portrait;
				dialog.PrintDocument(document.DocumentPaginator, jobName);
			});
		}

		/// <summary>Draws every page of the PDF to an image, first to last.</summary>
		/// <param name="pdf">The PDF file.</param>
		private static async Task<List<RenderedPage>> RenderPagesAsync(byte[] pdf)
		{
			using var source = new InMemoryRandomAccessStream();
			using (var writer = new DataWriter(source.GetOutputStreamAt(0)))
			{
				writer.WriteBytes(pdf);
				await writer.StoreAsync();
				await writer.FlushAsync();
				writer.DetachStream();
			}

			var document = await PdfDocument.LoadFromStreamAsync(source);
			var pages = new List<RenderedPage>();
			for (uint i = 0; i < document.PageCount; i++)
			{
				using var page = document.GetPage(i);
				double width = page.Size.Width, height = page.Size.Height;

				using var image = new InMemoryRandomAccessStream();
				await page.RenderToStreamAsync(image, new PdfPageRenderOptions
				{
					DestinationWidth = (uint)Math.Round(width * PrintDpi / UnitDpi),
					DestinationHeight = (uint)Math.Round(height * PrintDpi / UnitDpi)
				});
				pages.Add(new RenderedPage(await ToBitmapAsync(image), width, height));
			}
			return pages;
		}

		/// <summary>Reads the drawn page (PNG) into a frozen WPF bitmap.</summary>
		/// <param name="stream">The PNG written by the PDF engine.</param>
		private static async Task<BitmapSource> ToBitmapAsync(IRandomAccessStream stream)
		{
			var bytes = new byte[stream.Size];
			using (var reader = new DataReader(stream.GetInputStreamAt(0)))
			{
				await reader.LoadAsync((uint)stream.Size);
				reader.ReadBytes(bytes);
			}

			using var memory = new MemoryStream(bytes);
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.StreamSource = memory;
			bitmap.EndInit();
			bitmap.Freeze();
			return bitmap;
		}
	}
}
