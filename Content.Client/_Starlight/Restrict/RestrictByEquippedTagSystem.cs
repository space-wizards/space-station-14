using Content.Client.Popups;
using Content.Shared._Starlight.Restrict;
using Robust.Shared.Audio;

namespace Content.Client._Starlight.Restrict;

public sealed class RestrictByEquippedTagSystem : SharedRestrictByEquippedTagSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    protected override void PopupClient(string message, EntityUid user)
    {
        _popup.PopupCursor(message);
    }

    protected override void PlayDenialSound(SoundSpecifier? sound, EntityUid entity)
    {
        // Do nothing
    }
} 