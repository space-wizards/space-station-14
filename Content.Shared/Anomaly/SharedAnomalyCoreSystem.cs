using Content.Shared.Anomaly.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Anomaly;

/// <summary>
/// This component reduces the value of the entity during decay
/// </summary>
public sealed partial class SharedAnomalyCoreSystem : EntitySystem
{
    private static readonly EntityTimerId DecayTimer = new("decay");

    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CorePoweredThrowerComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);
        SubscribeLocalEvent<CorePoweredThrowerComponent, ExaminedEvent>(OnCorePoweredExamined);
        SubscribeLocalEvent<AnomalyCoreComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<AnomalyCoreComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<AnomalyCoreComponent> core, ref MapInitEvent args)
    {
        core.Comp.DecayMoment = _gameTiming.CurTime + TimeSpan.FromSeconds(core.Comp.TimeToDecay);
        RegisterTimer(core);
        Dirty(core, core.Comp);
    }

    private void OnHandleState(Entity<AnomalyCoreComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RegisterTimer(ent);
    }

    private void RegisterTimer(Entity<AnomalyCoreComponent> ent)
    {
        if (!ent.Comp.IsDecayed)
            _timers.SetTimerAt(ent, DecayTimer, ent.Comp.DecayMoment, flags: EntityTimerFlags.IgnoreEntityPause);
    }

    public TimeSpan GetRemainingTime(Entity<AnomalyCoreComponent> ent)
    {
        return _timers.TryGetTimer<AnomalyCoreComponent>(ent.Owner, DecayTimer, out var timer)
            ? timer.Remaining
            : TimeSpan.Zero;
    }

    private void OnTimer(Entity<AnomalyCoreComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == DecayTimer && !ent.Comp.IsDecayed)
            Decay(ent, ent.Comp);
    }

    private void OnAttemptMeleeThrowOnHit(Entity<CorePoweredThrowerComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        var (uid, comp) = ent;

        // don't waste charges on non-anchorable non-anomalous static bodies.
        if (!HasComp<AnomalyComponent>(args.Target)
            && !HasComp<AnchorableComponent>(args.Target)
            && TryComp<PhysicsComponent>(args.Target, out var body)
            && body.BodyType == BodyType.Static)
            return;

        args.Cancelled = true;
        args.Handled = true;

        if (!_itemSlots.TryGetSlot(uid, comp.CoreSlotId, out var slot))
            return;

        if (!TryComp<AnomalyCoreComponent>(slot.Item, out var coreComponent))
            return;

        if (coreComponent.IsDecayed)
        {
            if (coreComponent.Charge <= 0)
                return;
            args.Cancelled = false;
            coreComponent.Charge--;
        }
        else
        {
            args.Cancelled = false;
        }
    }

    private void OnCorePoweredExamined(Entity<CorePoweredThrowerComponent> ent, ref ExaminedEvent args)
    {
        var (uid, comp) = ent;
        if (!args.IsInDetailsRange)
            return;

        if (!_itemSlots.TryGetSlot(uid, comp.CoreSlotId, out var slot) ||
            !TryComp<AnomalyCoreComponent>(slot.Item, out var coreComponent))
        {
            args.PushMarkup(Loc.GetString("anomaly-gorilla-charge-none"));
            return;
        }

        if (coreComponent.IsDecayed)
        {
            args.PushMarkup(Loc.GetString("anomaly-gorilla-charge-limit", ("count", coreComponent.Charge)));
        }
        else
        {
            args.PushMarkup(Loc.GetString("anomaly-gorilla-charge-infinite"));
        }
    }

    private void Decay(EntityUid uid, AnomalyCoreComponent component)
    {
        _appearance.SetData(uid, AnomalyCoreVisuals.Decaying, false);
        component.IsDecayed = true;
        Dirty(uid, component);
    }
}
