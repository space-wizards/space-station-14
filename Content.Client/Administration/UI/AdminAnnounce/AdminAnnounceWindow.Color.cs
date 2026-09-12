using Content.Shared.Administration;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private string _currentHex = AdminAnnounceDefaults.DefaultColorHex;

    private void OnColorChanged()
    {
        UpdateColorPreview();
        SyncPalette();
    }

    private void OpenPalette()
    {
        if (_paletteWindow == null || _paletteWindow.Disposed)
        {
            _paletteWindow = new AdminAnnounceColorPalette();

            _paletteWindow.OnColorChanged += SetColor;
        }

        _paletteWindow.UpdateDisplay(GetCurrentColor());
        _paletteWindow.OpenCentered();
    }

    private void SyncPalette()
    {
        if (_paletteWindow == null || _paletteWindow.Disposed || !_paletteWindow.IsOpen)
            return;

        _paletteWindow.UpdateDisplay(GetCurrentColor());
    }

    private void SetColor(Color color)
    {
        var hex = color.ToHexNoAlpha();
        if (_currentHex == hex)
            return;

        _currentHex = hex;
        UpdateColorPreview();
        _paletteWindow?.UpdateDisplay(color);
    }

    private Color GetCurrentColor()
    {
        return AdminAnnounceHelpers.GetColor(GetSelectedAnnounceType(), _currentHex);
    }

    private void UpdateColorPreview()
    {
        ColorPreview.ModulateSelfOverride = GetCurrentColor();
    }
}
