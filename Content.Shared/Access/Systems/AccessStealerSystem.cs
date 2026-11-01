using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.Popups;

namespace Content.Shared.Access.Systems;

/// <summary>
/// Handles copying access from other ID's, used by the Agent ID.
/// </summary>
public sealed partial class AccessStealerSystem : EntitySystem
{
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<AccessComponent> _accessQuery;
    [Dependency] private EntityQuery<IdCardComponent> _idCardQuery;

    /// <summary>
    /// Steals access from interacted ids.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<AccessStealerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || _lock.IsLocked(ent.Owner) ||
            !_accessQuery.TryComp(args.Target, out var targetAccess) || !_idCardQuery.HasComp(args.Target))
            return;

        // Am I an id?
        if (!_accessQuery.TryComp(ent, out var access) || !_idCardQuery.HasComp(ent))
            return;

        var beforeLength = access.Tags.Count;
        access.Tags.UnionWith(targetAccess.Tags);
        var addedLength = access.Tags.Count - beforeLength;

        _popup.PopupEntity(Loc.GetString("agent-id-new", ("number", addedLength), ("card", args.Target)),
            args.Target.Value,
            args.User);
        if (addedLength > 0)
            Dirty(ent, access);
    }
}
