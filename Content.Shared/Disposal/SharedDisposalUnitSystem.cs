using Content.Shared.Body.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Emag.Systems;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Disposal;

[Serializable, NetSerializable]
public sealed class DisposalDoAfterEvent : SimpleDoAfterEvent
{
}

public abstract class SharedDisposalUnitSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected static TimeSpan ExitAttemptDelay = TimeSpan.FromSeconds(0.5);

    // Percentage
    public const float PressurePerSecond = 0.05f;

    /// <summary>
    /// Gets the current pressure state of a disposals unit.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public DisposalsPressureState GetState(EntityUid uid, DisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        var nextPressure = _metadata.GetPauseTime(uid, metadata) + component.NextPressurized;

        if (nextPressure > GameTiming.CurTime)
        {
            return DisposalsPressureState.Pressurizing;
        }

        return DisposalsPressureState.Ready;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisposalUnitComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<DisposalUnitComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<DisposalUnitComponent, GotEmaggedEvent>(OnEmagged);
    }

    public float GetPressure(EntityUid uid, DisposalUnitComponent component, MetaDataComponent? metadata = null)
    {
        if (!Resolve(uid, ref metadata))
            return 0f;

        var pauseTime = _metadata.GetPauseTime(uid, metadata);
        return MathF.Min(1f,
            (float) (GameTiming.CurTime - pauseTime - component.NextPressurized).TotalSeconds / PressurePerSecond);
    }

    private void OnPreventCollide(EntityUid uid, DisposalUnitComponent component,
        ref PreventCollideEvent args)
    {
        var otherBody = args.OtherEntity;

        // Items dropped shouldn't collide but items thrown should
        if (EntityManager.HasComponent<ItemComponent>(otherBody) &&
            !EntityManager.HasComponent<ThrownItemComponent>(otherBody))
        {
            args.Cancelled = true;
            return;
        }

        if (component.RecentlyEjected.Contains(otherBody))
        {
            args.Cancelled = true;
        }
    }

    private void OnCanDragDropOn(EntityUid uid, DisposalUnitComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanInsert(uid, component, args.Dragged);
        args.Handled = true;
    }

    private void OnEmagged(EntityUid uid, DisposalUnitComponent component, ref GotEmaggedEvent args)
    {
        component.DisablePressure = true;
        args.Handled = true;
    }

    public virtual bool CanInsert(EntityUid uid, DisposalUnitComponent component, EntityUid entity)
    {
        if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored)
            return false;

        // TODO: Probably just need a disposable tag.
        if (!EntityManager.TryGetComponent(entity, out ItemComponent? storable) &&
            !EntityManager.HasComponent<BodyComponent>(entity))
        {
            return false;
        }

        //Check if the entity is a mob and if mobs can be inserted
        if (TryComp<MobStateComponent>(entity, out var damageState) && !component.MobsCanEnter)
            return false;

        if (EntityManager.TryGetComponent(entity, out PhysicsComponent? physics) &&
            (physics.CanCollide || storable != null))
        {
            return true;
        }

        return damageState != null && (!component.MobsCanEnter || _mobState.IsDead(entity, damageState));
    }
}
