using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult.Abilities;

public abstract partial class SharedCosmicShiftSystem : EntitySystem
{
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;

    [Dependency] private IGameTiming _timing = default!;
    // [Dependency] private  ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    protected static readonly SoundSpecifier ShiftInSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/Abilities/ability-shift-in.ogg");
    private static readonly SoundSpecifier ShiftOutSfx = new SoundPathSpecifier("/Audio/_ST/CosmicCult/Abilities/ability-shift-out.ogg");
    private static readonly TimeSpan ShiftDuration = TimeSpan.FromSeconds(35);

    [SubscribeLocalEvent]
    private void OnReturnAbility(Entity<CosmicShiftedComponent> ent, ref EventCosmicReturn args)
    {
        ent.Comp.ReadyToReturn = true;
        _doAfter.Cancel(ent.Comp.ReturnDoAfter);
    }

    [SubscribeLocalEvent]
    private void OnShiftAbility(Entity<CosmicCultistComponent> ent, ref EventCosmicShift args)
    {
        if (args.Handled || HasComp<CosmicShiftedComponent>(ent) || HasComp<BlockMovementComponent>(ent) || _timing.ApplyingState || Container.IsEntityInContainer(ent.Owner))
            return;

        if (HasComp<BlockGridConstructionComponent>(Transform(ent).GridUid))
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.CosmicShiftWindup, new CosmicShiftStartDoAfter(), ent, ent)
        {
            DistanceThreshold = 1f, Hidden = false, BreakOnDamage = true, BreakOnMove = true, BreakOnDropItem = true,
        };
        args.Handled = _doAfter.TryStartDoAfter(doargs);
    }

    [SubscribeLocalEvent]
    protected virtual void OnShiftStartDoAfter(Entity<CosmicCultistComponent> ent, ref CosmicShiftStartDoAfter args)
    {
        if (args.Cancelled || args.Handled || Container.IsEntityInContainer(ent.Owner))
            return;

        TransformSystem.AnchorEntity(ent);
        EnsureComp<BlockMovementComponent>(ent);
        EnsureComp<CosmicShiftedComponent>(ent, out var shiftedComp);
        shiftedComp.DepartureCoordinates = TransformSystem.GetMapCoordinates(ent);
        shiftedComp.ReadyToReturn = false;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnShiftEndDoAfter(Entity<CosmicShiftedComponent> ent, ref CosmicShiftEndDoAfter args)
    {
        ent.Comp.ReadyToReturn = true;
    }

    public void ShiftToDestination(EntityUid ent, MapCoordinates destination)
    {
        OnShiftStart(ent);
        Audio.PlayPvs(ShiftInSfx, Transform(ent).Coordinates);
        // _entityTimer.SpawnMethodTimer(TimeSpan.FromSeconds(2.5), () => OnShiftMove(ent, destination)); // TODO: COSMIC CULT - ENTITY TIMERS
        // _entityTimer.SpawnMethodTimer(TimeSpan.FromSeconds(4.6), () => OnShiftEnd(ent)); // TODO: COSMIC CULT - ENTITY TIMERS
    }

    private void OnShiftStart(EntityUid ent)
    {
        RaiseNetworkEvent(new CosmicShiftAnimEvent(GetNetEntity(ent), CosmicShiftState.In));
    }

    protected virtual void OnShiftMove(EntityUid ent, MapCoordinates destination)
    {
        Audio.PlayPvs(ShiftOutSfx, Transform(ent).Coordinates);
        RaiseNetworkEvent(new CosmicShiftAnimEvent(GetNetEntity(ent), CosmicShiftState.Out));
    }

    protected virtual void OnShiftEnd(EntityUid ent)
    {
        RemComp<BlockMovementComponent>(ent);
        RaiseNetworkEvent(new CosmicShiftAnimEvent(GetNetEntity(ent), CosmicShiftState.Cancel));
        TransformSystem.Unanchor(ent);

        if (TryComp<CosmicShiftedComponent>(ent, out var shiftComp))
        {
            var doargs = new DoAfterArgs(EntityManager, ent, ShiftDuration, new CosmicShiftEndDoAfter(), ent, ent)
            {
                Hidden = true, BreakOnDamage = false, BreakOnMove = false, BreakOnDropItem = false, BreakOnHandChange = false,
            };
            _doAfter.TryStartDoAfter(doargs, out var doAfterId);
            shiftComp.ReturnDoAfter = doAfterId;
            Dirty(ent, shiftComp);
        }
    }

    [SubscribeLocalEvent]
    private void CancelDropEvent(EntityUid uid, CosmicShiftedComponent comp, DropAttemptEvent args)
    {
        args.Cancel();
    }
}
[Serializable, NetSerializable]
public sealed partial class CosmicShiftStartDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CosmicShiftEndDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class CosmicShiftAnimEvent : EntityEventArgs
{
    public NetEntity Target;

    public CosmicShiftState State;

    public CosmicShiftAnimEvent(NetEntity target, CosmicShiftState state)
    {
        Target = target;
        State =  state;
    }
}

[Serializable, NetSerializable]
public enum CosmicShiftState : byte
{
    In,
    Out,
    Cancel,
}
