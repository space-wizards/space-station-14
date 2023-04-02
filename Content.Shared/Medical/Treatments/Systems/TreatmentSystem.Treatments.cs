using Content.Shared.Medical.Treatments.Components;

namespace Content.Shared.Medical.Treatments.Systems;

public sealed partial class TreatmentSystem
{
    private void TreatmentListeners()
    {
        SubscribeLocalEvent<BleedTreatmentComponent, WoundTreatedEvent>(OnBleedTreatment);
        SubscribeLocalEvent<HealTreatmentComponent, WoundTreatedEvent>(OnHealTreatment);
        SubscribeLocalEvent<IntegrityTreatmentComponent, WoundTreatedEvent>(OnIntegrityTreatment);
    }

    private void OnIntegrityTreatment(EntityUid uid, IntegrityTreatmentComponent component, ref WoundTreatedEvent args)
    {

    }

    private void OnHealTreatment(EntityUid uid, HealTreatmentComponent component, ref WoundTreatedEvent args)
    {
    }

    private void OnBleedTreatment(EntityUid uid, BleedTreatmentComponent component, ref WoundTreatedEvent args)
    {
    }
}
