using Content.Shared.Administration.AdminAnnounce;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private void OpenPresets()
    {
        var presets = _presetWindow;
        if (presets == null || presets.Disposed)
        {
            presets = new AdminAnnouncePresetWindow();
            presets.OnPresetSelected += ApplyPreset;
            _presetWindow = presets;
        }

        presets.PopulatePresets();
        presets.OpenCentered();
    }

    private void ApplyPreset(AdminAnnouncementPresetPrototype preset)
    {
        SetColor(preset.Color);
        Announcer.Text = Loc.GetString(preset.Announcer);

        Signature.Text = preset.Signature is { } signature
            ? Loc.GetString(signature)
            : string.Empty;
        SoundPath.Text = preset.Sound?.Path.ToString() ?? string.Empty;
        Announcement.TextRope = new Rope.Leaf(preset.Message is { } message
            ? Loc.GetString(message)
            : string.Empty);

        UpdateControls();
    }
}
