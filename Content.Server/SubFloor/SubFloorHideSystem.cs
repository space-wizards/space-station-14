using Content.Shared.Construction.Components;
using Content.Shared.Eye;
using Content.Shared.SubFloor;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.SubFloor;

public sealed class SubFloorHideSystem : SharedSubFloorHideSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    private HashSet<ICommonSession> _showFloors = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubFloorHideComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<SubFloorHideComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeNetworkEvent<ShowSubfloorRequestEvent>(OnShowSubfloor);
        SubscribeLocalEvent<GetVisMaskEvent>(OnGetVisibility);

        _player.PlayerStatusChanged += OnPlayerStatus;
    }

    private void OnPlayerStatus(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
            return;

        _showFloors.Remove(e.Session);

        if (e.Session.AttachedEntity != null)
            _eye.RefreshVisibilityMask(e.Session.AttachedEntity.Value);
    }

    private void OnGetVisibility(ref GetVisMaskEvent ev)
    {
        if (!TryComp(ev.Entity, out ActorComponent? actor))
            return;

        if (_showFloors.Contains(actor.PlayerSession))
        {
            ev.VisibilityMask |= (int)VisibilityFlags.Subfloor;
        }
    }

    private void OnShowSubfloor(ShowSubfloorRequestEvent ev, EntitySessionEventArgs args)
    {
        // TODO: Commands are a bit of an eh? for client-only but checking shared perms
        var ent = args.SenderSession.AttachedEntity;

        if (!TryComp(ent, out EyeComponent? eyeComp))
            return;

        if (ev.Value)
        {
            _showFloors.Add(args.SenderSession);
        }
        else
        {
            _showFloors.Remove(args.SenderSession);
        }

        _eye.RefreshVisibilityMask((ent.Value, eyeComp));

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
