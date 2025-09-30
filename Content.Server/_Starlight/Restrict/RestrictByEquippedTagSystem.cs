using Content.Server.Popups;
using Content.Shared._Starlight.Restrict;

namespace Content.Server._Starlight.Restrict;

/// <summary>
/// Server-side implementation.
/// </summary>
public sealed class RestrictByEquippedTagSystem : SharedRestrictByEquippedTagSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    protected override void PopupClient(string message, EntityUid user)
    {
        _popup.PopupEntity(message, user, user);
    }
} 