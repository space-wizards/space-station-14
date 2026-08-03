// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Xenoborgs.Components;
using Content.Shared.DeadSpace.Xenoborgs;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.DeadSpace.Xenoborgs;

public sealed class MothershipEyeSystem : EntitySystem
{
    private const string EyePrototype = "XenoborgMothershipEye";

    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MothershipCoreComponent, ToggleMothershipEyeEvent>(OnToggleEye);
        SubscribeLocalEvent<MothershipCoreComponent, ComponentShutdown>(OnCoreShutdown);
        SubscribeLocalEvent<MothershipEyeComponent, MoveEvent>(OnEyeMoved);
        SubscribeLocalEvent<MothershipEyeComponent, ComponentShutdown>(OnEyeShutdown);
    }

    private void OnToggleEye(Entity<MothershipCoreComponent> ent, ref ToggleMothershipEyeEvent args)
    {
        if (TryFindEye(ent.Owner, out var existingEye))
        {
            ReleaseCore(ent.Owner, existingEye);
            QueueDel(existingEye);
            args.Handled = true;
            return;
        }

        var coreXform = Transform(ent.Owner);
        if (coreXform.GridUid is not { } gridUid || !HasComp<MapGridComponent>(gridUid))
            return;

        var eyeUid = Spawn(EyePrototype, coreXform.Coordinates);
        var eyeComp = EnsureComp<MothershipEyeComponent>(eyeUid);
        eyeComp.Core = ent.Owner;

        _eye.SetTarget(ent.Owner, eyeUid);
        _mover.SetRelay(ent.Owner, eyeUid);
        args.Handled = true;
    }

    private void OnCoreShutdown(Entity<MothershipCoreComponent> ent, ref ComponentShutdown args)
    {
        if (TryFindEye(ent.Owner, out var eyeUid))
            QueueDel(eyeUid);
    }

    private void OnEyeShutdown(Entity<MothershipEyeComponent> ent, ref ComponentShutdown args)
    {
        if (Exists(ent.Comp.Core) && !TerminatingOrDeleted(ent.Comp.Core))
            ReleaseCore(ent.Comp.Core, ent.Owner);
    }

    private void OnEyeMoved(Entity<MothershipEyeComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.RevertingMove ||
            TerminatingOrDeleted(ent.Owner) ||
            EntityManager.IsQueuedForDeletion(ent.Owner))
            return;

        if (!TryComp(ent.Comp.Core, out TransformComponent? coreXform) ||
            coreXform.GridUid is not { } coreGrid ||
            !_turf.TryGetTileRef(args.NewPosition, out var tile) ||
            tile.Value.GridUid != coreGrid ||
            tile.Value.Tile.IsEmpty ||
            _turf.IsSpace(tile.Value))
        {
            ent.Comp.RevertingMove = true;
            _transform.SetCoordinates(ent.Owner, args.OldPosition);
            ent.Comp.RevertingMove = false;
        }
    }

    private bool TryFindEye(EntityUid core, out EntityUid eyeUid)
    {
        var query = EntityQueryEnumerator<MothershipEyeComponent>();
        while (query.MoveNext(out var uid, out var eye))
        {
            if (eye.Core != core || TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            eyeUid = uid;
            return true;
        }

        eyeUid = default;
        return false;
    }

    private void ReleaseCore(EntityUid core, EntityUid eyeUid)
    {
        if (TryComp<EyeComponent>(core, out var eye) && eye.Target == eyeUid)
            _eye.SetTarget(core, null, eye);

        if (TryComp<RelayInputMoverComponent>(core, out var relay) && relay.RelayEntity == eyeUid)
            RemComp<RelayInputMoverComponent>(core);
    }
}
