using Content.Shared.Body.Part;
using Content.Shared.Medical.Treatments.Components;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Systems;

public sealed partial class TreatmentSystem : EntitySystem
{
    [Dependency] private WoundSystem _woundSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TreatmentComponent, ComponentGetState>(OnTreatmentGetState);
        SubscribeLocalEvent<TreatmentComponent, ComponentHandleState>(OnTreatmentHandleState);
        SubscribeLocalEvent<BleedTreatmentComponent, ComponentGetState>(OnBleedTreatmentGetState);
        SubscribeLocalEvent<BleedTreatmentComponent, ComponentHandleState>(OnBleedTreatmentHandleState);
        SubscribeLocalEvent<HealTreatmentComponent, ComponentGetState>(OnHealTreatmentGetState);
        SubscribeLocalEvent<HealTreatmentComponent, ComponentHandleState>(OnHealTreatmentHandleState);
        SubscribeLocalEvent<IntegrityTreatmentComponent, ComponentGetState>(OnIntegrityTreatmentGetState);
        SubscribeLocalEvent<IntegrityTreatmentComponent, ComponentHandleState>(OnIntegrityTreatmentHandleState);
        SubscribeLocalEvent<SeverityTreatmentComponent, ComponentGetState>(OnSeverityTreatmentGetState);
        SubscribeLocalEvent<SeverityTreatmentComponent, ComponentHandleState>(OnSeverityTreatmentHandleState);
        TreatmentListeners();
    }

    #region boilerplate

    private void OnSeverityTreatmentGetState(EntityUid uid, SeverityTreatmentComponent treatment,
        ref ComponentGetState args)
    {
        args.State = new SeverityTreatmentComponentState(
            treatment.IsModifier, treatment.SeverityChange);
    }

    private void OnIntegrityTreatmentGetState(EntityUid uid, IntegrityTreatmentComponent treatment,
        ref ComponentGetState args)
    {
        args.State = new IntegrityTreatmentComponentState(
            treatment.FullyRestores,
            treatment.RestoreAmount
        );
    }

    private void OnHealTreatmentGetState(EntityUid uid, HealTreatmentComponent treatment, ref ComponentGetState args)
    {
        args.State = new HealTreatmentComponentState(
            treatment.FullyHeals,
            treatment.LeavesScar,
            treatment.BaseHealingChange,
            treatment.HealingModifier,
            treatment.HealingMultiplier
        );
    }

    private void OnBleedTreatmentGetState(EntityUid uid, BleedTreatmentComponent treatment, ref ComponentGetState args)
    {
        args.State = new BleedTreatmentComponentState(
            treatment.FullyStopsBleed,
            treatment.BleedDecrease
        );
    }

    private void OnTreatmentGetState(EntityUid uid, TreatmentComponent treatment, ref ComponentGetState args)
    {
        args.State = new TreatmentComponentState(
            treatment.TreatmentType,
            treatment.LimitedUses,
            treatment.Uses,
            treatment.SelfUsable,
            treatment.TargetUsable
        );
    }

    private void OnSeverityTreatmentHandleState(EntityUid uid, SeverityTreatmentComponent treatment,
        ref ComponentHandleState args)
    {
        if (args.Current is not SeverityTreatmentComponentState state)
            return;
        treatment.IsModifier = state.IsModifier;
        treatment.SeverityChange = state.SeverityChange;
    }

    private void OnIntegrityTreatmentHandleState(EntityUid uid, IntegrityTreatmentComponent treatment,
        ref ComponentHandleState args)
    {
        if (args.Current is not IntegrityTreatmentComponentState state)
            return;
        treatment.FullyRestores = state.FullyRestores;
        treatment.RestoreAmount = state.RestoreAmount;
    }

    private void OnBleedTreatmentHandleState(EntityUid uid, BleedTreatmentComponent treatment,
        ref ComponentHandleState args)
    {
        if (args.Current is not BleedTreatmentComponentState state)
            return;
        treatment.BleedDecrease = state.BleedDecrease;
        treatment.FullyStopsBleed = state.FullyStopsBleed;
    }

    private void OnHealTreatmentHandleState(EntityUid uid, HealTreatmentComponent treatment,
        ref ComponentHandleState args)
    {
        if (args.Current is not HealTreatmentComponentState state)
            return;
        treatment.HealingModifier = state.HealingModifier;
        treatment.HealingMultiplier = state.HealingMultiplier;
        treatment.FullyHeals = state.FullyHeals;
        treatment.LeavesScar = state.LeavesScar;
        treatment.BaseHealingChange = state.BaseHealingChange;
    }

    private void OnTreatmentHandleState(EntityUid uid, TreatmentComponent treatment, ref ComponentHandleState args)
    {
        if (args.Current is not TreatmentComponentState state)
            return;
        treatment.TreatmentType = state.TreatmentType;
        treatment.Uses = state.Uses;
        treatment.LimitedUses = state.LimitedUses;
        treatment.SelfUsable = state.SelfUsable;
        treatment.TargetUsable = state.TargetUsable;
    }

    #endregion

    public bool CanApplyTreatment(EntityUid woundEntity, string treatmentId, WoundComponent? wound = null)
    {
        return Resolve(woundEntity, ref wound) && wound.ValidTreatments.Contains(treatmentId);
    }

    public bool TreatWound(EntityUid woundableEntity, EntityUid woundId, EntityUid treatmentEntity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null, TreatmentComponent? treatment = null)
    {
        if (!Resolve(woundableEntity, ref woundable) || !Resolve(woundId, ref wound) ||
            !Resolve(treatmentEntity, ref treatment))
            return false;
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
