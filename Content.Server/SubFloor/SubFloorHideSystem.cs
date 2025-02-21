using Content.Shared.Construction.Components;
using Content.Shared.Eye;
using Content.Shared.SubFloor;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;

namespace Content.Server.SubFloor;

public sealed class SubFloorHideSystem : SharedSubFloorHideSystem
{
    [Dependency] private readonly IConGroupController _console = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubFloorHideComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<SubFloorHideComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeNetworkEvent<ShowSubfloorRequestEvent>(OnShowSubfloor);
    }

    private void OnShowSubfloor(ShowSubfloorRequestEvent ev, EntitySessionEventArgs args)
    {
        // TODO: Commands are a bit of an eh? for client-only but checking shared perms
        var ent = args.SenderSession.AttachedEntity;

        if (!TryComp(ent, out EyeComponent? eyeComp))
            return;

        if (ev.Value)
        {
            _eye.SetVisibilityMask(ent.Value, eyeComp.VisibilityMask | (int) VisibilityFlags.Subfloor, eyeComp);
        }
        else
        {
            _eye.SetVisibilityMask(ent.Value, eyeComp.VisibilityMask & (int) ~VisibilityFlags.Subfloor, eyeComp);
        }

        RaiseNetworkEvent(new ShowSubfloorRequestEvent()
        {
            Value = ev.Value,
        }, args.SenderSession);
    }

    private void OnAnchorAttempt(EntityUid uid, SubFloorHideComponent component, AnchorAttemptEvent args)
    {
        // No teleporting entities through floor tiles when anchoring them.
        var xform = Transform(uid);

        if (TryComp<MapGridComponent>(xform.GridUid, out var grid)
            && HasFloorCover(xform.GridUid.Value, grid, Map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates)))
        {
            args.Cancel();
        }
    }

    private void OnUnanchorAttempt(EntityUid uid, SubFloorHideComponent component, UnanchorAttemptEvent args)
    {
        // No un-anchoring things under the floor. Only required for something like vents, which are still interactable
        // despite being partially under the floor.
        if (component.IsUnderCover)
            args.Cancel();
    }
}
