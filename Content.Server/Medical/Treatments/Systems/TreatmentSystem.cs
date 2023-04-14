using Content.Server.Medical.Bloodstream.Components;
using Content.Server.Medical.Bloodstream.Systems;
using Content.Server.Medical.Treatments.Components;
using Content.Shared.Body.Part;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Server.Medical.Treatments.Systems;

public sealed partial class TreatmentSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private WoundSystem _wound = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TreatBleedComponent, WoundTreatedEvent>(OnBleedTreatment);
        SubscribeLocalEvent<TreatHealthComponent, WoundTreatedEvent>(OnHealTreatment);
        SubscribeLocalEvent<TreatIntegrityComponent, WoundTreatedEvent>(OnIntegrityTreatment);
        SubscribeLocalEvent<TreatSeverityComponent, WoundTreatedEvent>(OnSeverityTreatment);
    }

    private void OnBleedTreatment(EntityUid uid, TreatBleedComponent component, ref WoundTreatedEvent args)
    {
        if (!TryComp(args.Part.Body, out BloodstreamComponent? bloodstream))
            return;

        // TODO fully stop bleed
        _bloodstream.ModifyBleedInflicter(args.WoundId, args.Part.Body.Value, component.Decrease, wound: args.Wound, bloodstream: bloodstream);
    }

    private void OnHealTreatment(EntityUid uid, TreatHealthComponent component, ref WoundTreatedEvent args)
    {
    }

    private void OnIntegrityTreatment(EntityUid uid, TreatIntegrityComponent component, ref WoundTreatedEvent args)
    {
    }


    private void OnSeverityTreatment(EntityUid uid, TreatSeverityComponent component, ref WoundTreatedEvent args)
    {
    }

    public bool CanApplyTreatment(EntityUid woundEntity, string treatmentId, WoundComponent? wound = null)
    {
        return Resolve(woundEntity, ref wound) && wound.ValidTreatments.Contains(treatmentId);
    }

    public bool TreatWound(
        EntityUid woundableId,
        EntityUid woundId,
        EntityUid treatmentEntity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null,
        TreatmentComponent? treatment = null,
        BodyPartComponent? woundablePart = null)
    {
        if (!Resolve(woundableId, ref woundable, ref woundablePart) ||
            !Resolve(woundId, ref wound) ||
            !Resolve(treatmentEntity, ref treatment))
        {
            return false;
        }

        var ev = new TreatWoundEvent();
        RaiseLocalEvent(treatmentEntity, ref ev, true);
        var ev2 = new WoundTreatedEvent(woundableId, woundId, woundable, wound, woundablePart);
        RaiseLocalEvent(woundId, ref ev2, true);

        //Relay the treatment event to the body entity
        if (woundablePart.Body.HasValue)
        {
            RaiseLocalEvent(woundablePart.Body.Value, ref ev2, true);
        }

        return false;
    }
}
