using MudBlazor;

namespace Vardiologio.App.Theme;

/// <summary>
/// The application's MudBlazor theme: colours, typography and layout sizes in one place.
/// Applied once by <c>MudThemeProvider</c> in <c>Components/Routes.razor</c>; components read it
/// through MudBlazor's CSS variables (e.g. <c>var(--mud-palette-primary)</c>), which the
/// app's own stylesheet (<c>wwwroot/app.css</c>) uses as well.
/// </summary>
public static class AppTheme
{
	/// <summary>
	/// Font stack of every text style. Manrope is bundled in <c>wwwroot/fonts</c> (the app works
	/// offline, so nothing is loaded from the internet); Segoe UI is the Windows fallback.
	/// </summary>
	private static readonly string[] Fonts = ["Manrope", "Segoe UI", "sans-serif"];

	/// <summary>The theme instance handed to <c>MudThemeProvider</c>.</summary>
	public static readonly MudTheme Theme = new()
	{
		PaletteLight = new PaletteLight
		{
			// Brand
			Primary = "#4338CA",
			PrimaryContrastText = "#FFFFFF",
			Secondary = "#2F279C",
			SecondaryContrastText = "#FFFFFF",

			// Status colours (dark enough for white text and for text on a light tint)
			Info = "#1D5FBF",
			Success = "#17693F",
			Warning = "#B45309",
			Error = "#B3261E",

			// Surfaces
			Background = "#F5F4FB",
			BackgroundGray = "#ECEBFB",
			Surface = "#FFFFFF",
			AppbarBackground = "#FFFFFF",
			AppbarText = "#1D1B2E",
			DrawerBackground = "#F5F4FB",
			DrawerText = "#3B3852",
			DrawerIcon = "#5C5A73",

			// Text
			TextPrimary = "#1D1B2E",
			TextSecondary = "#5C5A73",
			ActionDefault = "#5C5A73",

			// Lines
			LinesDefault = "#E3E1EF",
			LinesInputs = "#CFCCE2",
			TableLines = "#E3E1EF",
			Divider = "#E3E1EF"
		},

		Typography = new Typography
		{
			// Every style names the font: MudBlazor does not inherit the family from Default.
			Default = new DefaultTypography { FontFamily = Fonts },
			H1 = new H1Typography { FontFamily = Fonts, FontWeight = "800" },
			H2 = new H2Typography { FontFamily = Fonts, FontWeight = "800" },
			H3 = new H3Typography { FontFamily = Fonts, FontWeight = "800" },
			// h4 = page title, h5 = section title, h6 = card title / brand.
			H4 = new H4Typography { FontFamily = Fonts, FontWeight = "800", FontSize = "1.75rem", LetterSpacing = "-.02em" },
			H5 = new H5Typography { FontFamily = Fonts, FontWeight = "800", FontSize = "1.375rem", LetterSpacing = "-.01em" },
			H6 = new H6Typography { FontFamily = Fonts, FontWeight = "800", FontSize = "1.125rem", LetterSpacing = "-.01em" },
			Subtitle1 = new Subtitle1Typography { FontFamily = Fonts, FontWeight = "600" },
			Subtitle2 = new Subtitle2Typography { FontFamily = Fonts, FontWeight = "700" },
			Body1 = new Body1Typography { FontFamily = Fonts, FontSize = ".9375rem" },
			Body2 = new Body2Typography { FontFamily = Fonts },
			// Buttons keep the text as typed (MudBlazor's default is upper case).
			Button = new ButtonTypography { FontFamily = Fonts, FontWeight = "700", TextTransform = "none", LetterSpacing = "0" },
			Caption = new CaptionTypography { FontFamily = Fonts },
			Overline = new OverlineTypography { FontFamily = Fonts, FontWeight = "700" }
		},

		LayoutProperties = new LayoutProperties
		{
			DefaultBorderRadius = "12px",
			DrawerWidthLeft = "264px",
			AppbarHeight = "64px"
		}
	};
}
