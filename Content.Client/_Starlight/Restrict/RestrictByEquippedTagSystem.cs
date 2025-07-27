using Content.Client.Popups;
using Content.Shared._Starlight.Restrict;

namespace Content.Client._Starlight.Restrict;

public sealed class RestrictByEquippedTagSystem : SharedRestrictByEquippedTagSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    protected override void PopupClient(string message, EntityUid user)
    {
        _popup.PopupCursor(message);
    }
} 