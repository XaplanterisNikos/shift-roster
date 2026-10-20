using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vardiologio.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Title bar: application name and the running version, e.g. "Shift-Roster v0.7.0"
        // (the XAML title is only what the designer shows).
        Title = AppInfo.WindowTitle;
    }

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		var area = SystemParameters.WorkArea;

		Left = area.Left;
		Top = area.Top;
		Width = area.Width;
		Height = area.Height;
	}
}