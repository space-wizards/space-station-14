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
public sealed class SharedAnomalyCoreSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyCoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CorePoweredThrowerComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);
        SubscribeLocalEvent<CorePoweredThrowerComponent, ExaminedEvent>(OnCorePoweredExamined);
    }

    private void OnMapInit(Entity<AnomalyCoreComponent> core, ref MapInitEvent args)
    {
        core.Comp.DecayMoment = _gameTiming.CurTime + TimeSpan.FromSeconds(core.Comp.TimeToDecay);
        Dirty(core, core.Comp);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnomalyCoreComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.IsDecayed)
                continue;

            //When time runs out, we completely decompose
            if (component.DecayMoment < _gameTiming.CurTime)
                Decay(uid, component);
        }
    }

    private void Decay(EntityUid uid, AnomalyCoreComponent component)
    {
        _appearance.SetData(uid, AnomalyCoreVisuals.Decaying, false);
        component.IsDecayed = true;
        Dirty(uid, component);
    }
}
