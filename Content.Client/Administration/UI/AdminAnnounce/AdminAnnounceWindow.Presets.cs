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
        ResetPresetFields();

        SetColor(preset.Color);
        Announcer.Text = Loc.GetString(preset.Announcer);

        if (preset.Signature is { } signature)
        {
            Signature.Text = Loc.GetString(signature);
            EnableSignature.Pressed = true;
        }

        if (preset.Sound is { } sound)
            SoundPath.Text = sound.Path.ToString();

        if (preset.Message is { } message)
            Announcement.TextRope = new Rope.Leaf(Loc.GetString(message));

        UpdateSignatureEditable();
        UpdateButtons();
    }

    private void ResetPresetFields()
    {
        Signature.Text = string.Empty;
        EnableSignature.Pressed = false;
        SoundPath.Text = string.Empty;
    }
}
