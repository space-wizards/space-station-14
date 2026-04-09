using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private void TogglePreview()
    {
        if (IsStreamPlaying(_previewStream))
            StopPreview();
        else if (!string.IsNullOrWhiteSpace(SoundPath.Text))
            _previewStream = _audio?.PlayGlobal(SoundPath.Text, Filter.Local(), false)?.Entity;
        UpdateButtons();
    }

    private void StopPreview()
    {
        if (_previewStream == null) return;
        _audio?.Stop(_previewStream);
        _previewStream = null;
    }

    private static bool IsStreamPlaying(EntityUid? stream)
    {
        return stream != null && stream.Value.IsValid() &&
               IoCManager.Resolve<IEntityManager>().EntityExists(stream.Value);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var isPreviewing = IsStreamPlaying(_previewStream);
        AnnounceButton.Disabled = string.IsNullOrWhiteSpace(Rope.Collapse(Announcement.TextRope));

        var type = (AdminAnnounceType?)AnnounceMethod.SelectedMetadata;
        PlayAudio.Disabled = type != AdminAnnounceType.Station;
        PlayAudio.Text = isPreviewing ? "⏹" : "▶";
    }

    private void UpdateFields(AdminAnnounceType type)
    {
        var isStation = type == AdminAnnounceType.Station;
        Announcer.Editable = Sender.Editable = SoundPath.Editable = isStation;

        _currentHex = isStation
            ? AdminAnnounceDefaults.DefaultColorHex
            : AdminAnnounceDefaults.ServerColorHex;

        OnColorChanged();

        if (!isStation) StopPreview();
        UpdateButtons();
    }
}