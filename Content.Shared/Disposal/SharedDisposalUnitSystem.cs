using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public sealed partial class DisposalDoAfterEvent : SimpleDoAfterEvent
{
}

public abstract class SharedDisposalUnitSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly EmagSystem _emag = default!;
    [Dependency] protected readonly MetaDataSystem Metadata = default!;
    [Dependency] protected readonly SharedJointSystem Joints = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

    // Percentage
    public const float PressurePerSecond = 0.05f;

    public abstract bool HasDisposals([NotNullWhen(true)] EntityUid? uid);

    public abstract bool ResolveDisposals(EntityUid uid, [NotNullWhen(true)] ref SharedDisposalUnitComponent? component);

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public DisposalsPressureState GetState(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        var nextPressure = Metadata.GetPauseTime(uid, metadata) + component.NextPressurized - GameTiming.CurTime;
        var pressurizeTime = 1f / PressurePerSecond;
        var pressurizeDuration = pressurizeTime - component.FlushDelay.TotalSeconds;

        if (nextPressure.TotalSeconds > pressurizeDuration)
        {
            return DisposalsPressureState.Flushed;
        }

        if (nextPressure > TimeSpan.Zero)
        {
            return DisposalsPressureState.Pressurizing;
        }

        return DisposalsPressureState.Ready;
    }

    public float GetPressure(EntityUid uid, SharedDisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata))
            return 0f;

        var pauseTime = Metadata.GetPauseTime(uid, metadata);
        return MathF.Min(1f,
            (float) (GameTiming.CurTime - pauseTime - component.NextPressurized).TotalSeconds / PressurePerSecond);
    }

    protected void OnPreventCollide(EntityUid uid, SharedDisposalUnitComponent component,
        ref PreventCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        // Items dropped shouldn't collide but items thrown should
        if (HasComp<ItemComponent>(otherBody) && !HasComp<ThrownItemComponent>(otherBody))
        {
            args.Cancelled = true;
            return;
        }

        if (component.RecentlyEjected.Contains(otherBody))
        {
            args.Cancelled = true;
        }
    }

    protected void OnCanDragDropOn(EntityUid uid, SharedDisposalUnitComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanInsert(uid, component, args.Dragged);
        args.Handled = true;
    }

    protected void OnEmagged(EntityUid uid, SharedDisposalUnitComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (component.DisablePressure == true)
            return;

        component.DisablePressure = true;
        args.Handled = true;
    }

    public virtual bool CanInsert(EntityUid uid, SharedDisposalUnitComponent component, EntityUid entity)
    {
        if (!Transform(uid).Anchored)
            return false;

        var storable = HasComp<ItemComponent>(entity);
        if (!storable && !HasComp<BodyComponent>(entity))
            return false;

        if (_whitelistSystem.IsBlacklistPass(component.Blacklist, entity) ||
            _whitelistSystem.IsWhitelistFail(component.Whitelist, entity))
            return false;

        if (TryComp<PhysicsComponent>(entity, out var physics) && (physics.CanCollide) || storable)
            return true;
        else
            return false;

    }

    public abstract void DoInsertDisposalUnit(EntityUid uid, EntityUid toInsert, EntityUid user, SharedDisposalUnitComponent? disposal = null);

    [Serializable, NetSerializable]
    protected sealed class DisposalUnitComponentState : ComponentState
    {
        public SoundSpecifier? FlushSound;
        public DisposalsPressureState State;
        public TimeSpan NextPressurized;
        public TimeSpan AutomaticEngageTime;
        public TimeSpan? NextFlush;
        public bool Powered;
        public bool Engaged;
        public List<NetEntity> RecentlyEjected;

        public DisposalUnitComponentState(SoundSpecifier? flushSound, DisposalsPressureState state, TimeSpan nextPressurized, TimeSpan automaticEngageTime, TimeSpan? nextFlush, bool powered, bool engaged, List<NetEntity> recentlyEjected)
        {
            FlushSound = flushSound;
            State = state;
            NextPressurized = nextPressurized;
            AutomaticEngageTime = automaticEngageTime;
            NextFlush = nextFlush;
            Powered = powered;
            Engaged = engaged;
            RecentlyEjected = recentlyEjected;
        }
    }
}
