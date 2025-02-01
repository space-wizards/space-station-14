using Content.Shared.ItemRecall;
using Robust.Server.GameStates;
using Robust.Server.Player;

namespace Content.Server.ItemRecall;

/// <summary>
/// System for handling the ItemRecall ability for wizards.
/// </summary>
public sealed partial class ItemRecallSystem : SharedItemRecallSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    protected override void AddToPVSOverride(EntityUid uid, EntityUid user)
    {
        if (!_player.TryGetSessionByEntity(user, out var mindSession))
            return;

        _pvs.AddSessionOverride(uid, mindSession);
    }

    protected override void RemoveFromPVSOverride(EntityUid uid, EntityUid user)
    {
        if (!_player.TryGetSessionByEntity(user, out var mindSession))
            return;

        _pvs.RemoveSessionOverride(uid, mindSession);
    }
}
