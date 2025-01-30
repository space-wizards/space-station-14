using Content.Shared.ItemRecall;
using Robust.Server.GameStates;

namespace Content.Server.ItemRecall;

/// <summary>
/// System for handling the ItemRecall ability for wizards.
/// </summary>
public sealed partial class ItemRecallSystem : SharedItemRecallSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    protected override void AddToPVSOverride(EntityUid uid, EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var _, out var mind))
            return;

        if (mind.Session != null)
            _pvs.AddSessionOverride(uid, mind.Session);
    }

    protected override void RemoveFromPVSOverride(EntityUid uid, EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var _, out var mind))
            return;

        if (mind.Session != null)
            _pvs.RemoveSessionOverride(uid, mind.Session);
    }
}
