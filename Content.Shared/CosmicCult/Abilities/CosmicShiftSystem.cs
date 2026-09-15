using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
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

public abstract partial class CosmicShiftSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier ShiftInSfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-shift-in.ogg");
    private static readonly SoundSpecifier ShiftOutSfx = new SoundPathSpecifier("/Audio/Cosmic/Abilities/ability-shift-out.ogg");
    private static readonly TimeSpan ShiftDuration = TimeSpan.FromSeconds(35);


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var shiftingQuery = EntityQueryEnumerator<CosmicShiftingComponent>();
        while (shiftingQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.ShiftMoveTimer is { } moveTimer && _timing.CurTime >= moveTimer)
            {
                comp.ShiftMoveTimer = null;
                EnsureComp<CosmicShiftingComponent>(uid, out var shiftComp);
                OnShiftMove(uid, shiftComp.DestinationCoordinates);
                Dirty(uid, comp);
            }

            if (comp.ShiftEndTimer is { } endTimer && _timing.CurTime >= endTimer)
            {
                comp.ShiftEndTimer = null;
                RemComp<CosmicShiftingComponent>(uid);
                OnShiftEnd(uid);
                Dirty(uid, comp);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnReturnAbility(Entity<CosmicActionReturnComponent> ent, ref EventCosmicReturn args)
    {
        if (TryComp<CosmicShiftedComponent>(args.Performer, out var shiftedComp))
        {
            shiftedComp.ReadyToReturn = true;
            DoAfter.Cancel(shiftedComp.ReturnDoAfter);
        }
    }

    [SubscribeLocalEvent]
    protected virtual void OnShiftAbility(Entity<CosmicActionShiftComponent> ent, ref EventCosmicShift args)
    {
        if (args.Handled || HasComp<CosmicShiftedComponent>(args.Performer) || Container.IsEntityInContainer(args.Performer))
            return;

        if (HasComp<BlockGridConstructionComponent>(Transform(args.Performer).GridUid))
            return;

        EnsureComp<CosmicShiftedComponent>(args.Performer, out var shiftedComp);
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
        _audio.PlayPvs(ShiftInSfx, Transform(ent).Coordinates);
        EnsureComp<CosmicShiftingComponent>(ent, out var shiftComp);
        shiftComp.DestinationCoordinates = destination;
        shiftComp.ShiftMoveTimer = _timing.CurTime + TimeSpan.FromSeconds(2.5f);
        shiftComp.ShiftEndTimer = _timing.CurTime + TimeSpan.FromSeconds(4.6f);
    }

    private void OnShiftStart(EntityUid ent)
    {
        RaiseNetworkEvent(new CosmicShiftAnimEvent(GetNetEntity(ent), CosmicShiftState.In));
    }

    protected virtual void OnShiftMove(EntityUid ent, MapCoordinates destination)
    {
        _audio.PlayPvs(ShiftOutSfx, Transform(ent).Coordinates);
        RaiseNetworkEvent(new CosmicShiftAnimEvent(GetNetEntity(ent), CosmicShiftState.Out));
    }

    protected virtual void OnShiftEnd(EntityUid ent)
    {
        RaiseNetworkEvent(new CosmicShiftAnimEvent(GetNetEntity(ent), CosmicShiftState.Cancel));

        if (TryComp<CosmicShiftedComponent>(ent, out var shiftComp))
        {
            var doargs = new DoAfterArgs(EntityManager, ent, ShiftDuration, new CosmicShiftEndDoAfter(), ent, ent)
            {
                Hidden = true, BreakOnDamage = false, BreakOnMove = false, BreakOnDropItem = false, BreakOnHandChange = false, RequireCanInteract = false,
            };
            DoAfter.TryStartDoAfter(doargs, out var doAfterId);
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
