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

            _paletteWindow.OnPickerChanged += color =>
            {
                var hex = color.ToHexNoAlpha();
                if (_currentHex == hex) return;
                
                _currentHex = hex;
                UpdateColorPreview();
                _paletteWindow.SetHexText(hex);
            };

            _paletteWindow.OnHexChanged += hex =>
            {
                if (string.IsNullOrEmpty(hex) || _currentHex == hex) return;
                _currentHex = hex;
                UpdateColorPreview();
            };
        }

        _paletteWindow.UpdateDisplay(GetCurrentColor(), _currentHex);
        _paletteWindow.OpenCentered();
    }

    private void SyncPalette()
    {
        if (_paletteWindow == null || _paletteWindow.Disposed || !_paletteWindow.IsOpen) 
            return;

        _paletteWindow.UpdateDisplay(GetCurrentColor(), _currentHex);
    }

    private Color GetCurrentColor()
    {
        var type = (AdminAnnounceType?)AnnounceMethod.SelectedMetadata ?? AdminAnnounceType.Station;
        return AdminAnnounceHelpers.GetColor(type, _currentHex);
    }

    private void UpdateColorPreview()
    {
        ColorPreview.ModulateSelfOverride = GetCurrentColor();
    }

    public string GetCurrentHex() => _currentHex;
}
