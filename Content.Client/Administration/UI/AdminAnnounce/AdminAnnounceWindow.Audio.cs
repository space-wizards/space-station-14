using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.AdminAnnounce;

public sealed partial class AdminAnnounceWindow
{
    private void PlayPreview()
    {
        StopPreview();

        if (GetSound() is not { } sound)
            return;

        _previewStream = _audio
            .PlayGlobal(sound, Filter.Local(), false)?
            .Entity;
    }

    private void StopPreview()
    {
        _previewStream = _audio.Stop(_previewStream);
    }

    private SoundPathSpecifier? GetSound()
    {
        var value = SoundPath.Text.Trim();
        if (string.IsNullOrEmpty(value) || !ResPath.IsValidPath(value))
            return null;

        var path = new ResPath(value);
        var sound = new SoundPathSpecifier(path);
        return AudioHelpers.IsValidContentSound(sound, _resourceManager)
            ? sound
            : null;
    }
}
