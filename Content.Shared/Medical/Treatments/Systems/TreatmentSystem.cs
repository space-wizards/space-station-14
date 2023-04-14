using Content.Shared.Body.Part;
using Content.Shared.Medical.Treatments.Components;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Shared.Medical.Treatments.Systems;

public sealed partial class TreatmentSystem : EntitySystem
{
    [Dependency] private WoundSystem _woundSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TreatBleedComponent, WoundTreatedEvent>(OnBleedTreatment);
        SubscribeLocalEvent<TreatHealthComponent, WoundTreatedEvent>(OnHealTreatment);
        SubscribeLocalEvent<TreatIntegrityComponent, WoundTreatedEvent>(OnIntegrityTreatment);
    }

    private void OnIntegrityTreatment(EntityUid uid, TreatIntegrityComponent component, ref WoundTreatedEvent args)
    {
    }

    private void OnHealTreatment(EntityUid uid, TreatHealthComponent component, ref WoundTreatedEvent args)
    {
    }

    private void OnBleedTreatment(EntityUid uid, TreatBleedComponent component, ref WoundTreatedEvent args)
    {
    }

    public bool CanApplyTreatment(EntityUid woundEntity, string treatmentId, WoundComponent? wound = null)
    {
        return Resolve(woundEntity, ref wound) && wound.ValidTreatments.Contains(treatmentId);
    }

    public bool TreatWound(EntityUid woundableEntity, EntityUid woundId, EntityUid treatmentEntity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null, TreatmentComponent? treatment = null)
    {
        if (!Resolve(woundableEntity, ref woundable) ||
            !Resolve(woundId, ref wound) ||
            !Resolve(treatmentEntity, ref treatment))
        {
            return false;
        }

        var ev = new TreatWoundEvent();
        RaiseLocalEvent(treatmentEntity, ref ev, true);
        var ev2 = new WoundTreatedEvent(woundId, wound);
        RaiseLocalEvent(woundId, ref ev2, true);

        //Relay the treatment event to the body entity
        if (TryComp<BodyPartComponent>(woundableEntity, out var bodyPart) && bodyPart.Body.HasValue)
        {
            RaiseLocalEvent(bodyPart.Body.Value, ref ev2, true);
        }

        return false;
    }
}
