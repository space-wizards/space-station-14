namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private Color _announcementColor;

    private void OpenPalette()
    {
        var palette = _paletteWindow;
        if (palette == null || palette.Disposed)
        {
            palette = new AdminAnnounceColorPalette();
            palette.OnColorChanged += SetColor;
            _paletteWindow = palette;
        }

        palette.UpdateDisplay(_announcementColor);
        palette.OpenCentered();
    }

    private void SetColor(Color color)
    {
        _announcementColor = color;
        ColorPreview.ModulateSelfOverride = color;
    }
}
