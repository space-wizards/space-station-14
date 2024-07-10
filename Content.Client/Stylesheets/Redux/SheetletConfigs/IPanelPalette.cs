namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface IPanelPalette : ISheetletConfig
{
    public Color PanelDarkColor { get; }
    /// <summary>
    ///     Color used for window backgrounds.
    /// </summary>
    public Color PanelColor { get; }
    public Color PanelLightColor { get;  }
}
