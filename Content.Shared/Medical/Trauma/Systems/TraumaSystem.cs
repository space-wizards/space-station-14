using Content.Shared.FixedPoint;
using Content.Shared.Medical.Trauma.Components;
using Content.Shared.Medical.Wounding.Events;

namespace Content.Shared.Medical.Trauma.Systems;

public sealed partial class TraumaSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HealthTraumaComponent,WoundCreatedEvent>(ApplyHealthTrauma);
        SubscribeLocalEvent<IntegrityTraumaComponent,WoundCreatedEvent>(ApplyIntegrityTrauma);
    }

    private void ApplyIntegrityTrauma(EntityUid uid, IntegrityTraumaComponent trauma, ref WoundCreatedEvent args)
    {
        args.ParentWoundable.Comp.IntegrityCap =
            FixedPoint2.Clamp(
                args.ParentWoundable.Comp.IntegrityCap - trauma.IntegrityCapDecrease,
                0,
                args.ParentWoundable.Comp.MaxIntegrity);

        //TODO: update woundable integrity and raise appropriate events

        Dirty(args.ParentWoundable);
    }

    private void ApplyHealthTrauma(EntityUid uid, HealthTraumaComponent trauma, ref WoundCreatedEvent args)
    {
        args.ParentWoundable.Comp.HealthCap =
            FixedPoint2.Clamp(
                args.ParentWoundable.Comp.HealthCap - trauma.HealthCapDecrease,
                0,
                args.ParentWoundable.Comp.MaxHealth);
        Dirty(args.ParentWoundable);
    }
}
