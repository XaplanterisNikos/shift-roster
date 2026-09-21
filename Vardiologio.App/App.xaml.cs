using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Vardiologio.Application.Auth;
using Vardiologio.Application.Employees;
using Vardiologio.Application.Reports;
using Vardiologio.Application.ShiftCodes;
using Vardiologio.Application.ShiftEntry;
using Vardiologio.Infrastructure.Auth;
using Vardiologio.Infrastructure.Employees;
using Vardiologio.Infrastructure.Persistence;
using Vardiologio.Infrastructure.Reports;
using Vardiologio.Infrastructure.ShiftCodes;
using Vardiologio.Infrastructure.ShiftEntry;

namespace Vardiologio.App;

/// <summary>
/// WPF application entry point and composition root.
/// On startup it wires global exception logging, prepares the database
/// (migrations + seed) and builds the dependency-injection container that
/// the Blazor UI (hosted in MainWindow's BlazorWebView) resolves services from.
/// </summary>
public partial class App : System.Windows.Application
{
	/// <summary>
	/// Runs once when the app launches (before any window is shown): sets up error
	/// logging, the QuestPDF licence, the database, and the DI service provider,
	/// then lets WPF create the main window. Any startup failure is logged and the
	/// app shuts down cleanly.
	/// </summary>
	protected override void OnStartup(StartupEventArgs e)
	{
		// Catch and log any unhandled exception (UI thread + any thread) so it is never lost.
		DispatcherUnhandledException += (_, args) => { LogFatal(args.Exception); args.Handled = true; };
		AppDomain.CurrentDomain.UnhandledException += (_, args) => LogFatal(args.ExceptionObject as Exception);

		try
		{
			// QuestPDF requires its (free) Community licence to be declared once at startup.
			QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

			DbInitializer.Initialize(); // apply migrations + seed

			var services = new ServiceCollection();
			services.AddWpfBlazorWebView();
#if DEBUG
			services.AddBlazorWebViewDeveloperTools();
#endif
			services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(AppPaths.ConnectionString));

			// Application services
			services.AddScoped<IShiftEntryService, ShiftEntryService>();
			services.AddScoped<IReportService, ReportService>();
			services.AddScoped<IIndividualReportService, IndividualReportService>();
			services.AddScoped<IEmployeeService, EmployeeService>();
			services.AddScoped<IShiftCodeService, ShiftCodeService>();

			// Report renderers
			services.AddSingleton<IExcelReportRenderer, ClosedXmlReportRenderer>();
			services.AddSingleton<IPdfReportRenderer, QuestPdfReportRenderer>();   // PDF report output

			// Auth
			services.AddSingleton<IAuthService, AuthService>();
			services.AddSingleton<IUserSession, UserSession>();

			Resources.Add("services", services.BuildServiceProvider());

			base.OnStartup(e); // creates MainWindow — kept inside try so startup errors are logged too
		}
		catch (Exception ex)
		{
			LogFatal(ex);
			Shutdown();
		}
	}

	/// <summary>Writes the exception to %LOCALAPPDATA%\Vardiologio\startup-error.log and shows a dialog.</summary>
	private static void LogFatal(Exception? ex)
	{
		try
		{
			var dir = System.IO.Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vardiologio");
			System.IO.Directory.CreateDirectory(dir);
			System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "startup-error.log"), $"{DateTime.Now:u}\n\n{ex}");
			MessageBox.Show(ex?.Message ?? "Unknown error", "Σφάλμα — δες startup-error.log");
		}
		catch { /* last resort */ }
	}
}