using System.Collections.Immutable;
using Content.Server.Atmos.Components;
using Content.Server.CosmicCult.Components;
using Content.Shared.Actions;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Abilities;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class ServerCosmicShiftSystem : CosmicShiftSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shiftedQuery = EntityQueryEnumerator<CosmicShiftedComponent>();
        while (shiftedQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ReadyToReturn && !comp.Occupied)
            {
                _actions.RemoveAction(uid, comp.CosmicReturnActionActionEntity);
                _status.TryAddStatusEffectDuration(uid, SharedStunSystem.StunId, TimeSpan.FromSeconds(5f));
                RemComp<CosmicShiftedComponent>(uid);

                ShiftToDestination(uid, comp.DepartureCoordinates);
                foreach (var entity in _lookup.GetEntitiesIntersecting(comp.DepartureCoordinates, LookupFlags.Static))
                {
                    if (HasComp<AirtightComponent>(entity) && !HasComp<AirlockComponent>(entity))
                        QueueDel(entity);
                }
            }

            if (comp.AutoReturnTimer is { } returnTimer && _timing.CurTime >= returnTimer)
            {
                DoAfter.Cancel(comp.ReturnDoAfter);
                comp.AutoReturnTimer = null;
                comp.ReadyToReturn = true;
                comp.Occupied = false;
            }
        }
    }

    protected override void OnShiftAbility(Entity<CosmicActionShiftComponent> ent, ref EventCosmicShift args)
    {
        if (Container.IsEntityInContainer(args.Performer))
            return;

        var spawnPoints = EntityManager.GetAllComponents(typeof(CosmicVoidSpawnComponent)).ToImmutableList();
        if (spawnPoints.IsEmpty)
            return;

        var destination = TransformSystem.GetMapCoordinates(_random.Pick(spawnPoints).Uid);
        _popup.PopupCoordinates(Loc.GetString("cosmicability-shift-start", ("target", Identity.Entity(args.Performer, EntityManager))), Transform(args.Performer).Coordinates, PopupType.MediumCaution);
        _status.TryAddStatusEffectDuration(args.Performer, SharedStunSystem.StunId, TimeSpan.FromSeconds(5f));

        EnsureComp<CosmicShiftedComponent>(args.Performer, out var shiftedComp);
        EnsureComp<CosmicShiftingComponent>(args.Performer, out var shiftingComp);
        ShiftToDestination(args.Performer, destination);
        shiftedComp.DepartureCoordinates = TransformSystem.GetMapCoordinates(args.Performer);
        shiftingComp.DestinationCoordinates = destination;
        shiftedComp.ReadyToReturn = false;
        base.OnShiftAbility(ent, ref args);
    }

    protected override void OnShiftMove(EntityUid ent, MapCoordinates destination)
    {
        TransformSystem.SetMapCoordinates(ent, destination);
        base.OnShiftMove(ent, destination);
    }

    protected override void OnShiftEnd(EntityUid ent)
    {
        base.OnShiftEnd(ent);
        if (TryComp<CosmicShiftedComponent>(ent, out var shiftComp))
            _actions.AddAction(ent, ref shiftComp.CosmicReturnActionActionEntity, shiftComp.CosmicReturnAction, ent);
    }
}
