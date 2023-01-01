using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.ReactionEffects;
using Content.Server.Fluids.EntitySystems;
using Content.Server.HealthExaminable;
using Content.Server.MobState;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Drunk;
using Content.Shared.Rejuvenate;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SpillableSystem _spillableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;

    // TODO here
    // Update over time. Modify bloodloss damage in accordance with (amount of blood / max blood level), and reduce bleeding over time
    // Sub to damage changed event and modify bloodloss if incurring large hits of slashing/piercing

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodstreamComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
        SubscribeLocalEvent<BloodstreamComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<BloodstreamComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BloodstreamComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnReactionAttempt(EntityUid uid, BloodstreamComponent component, ReactionAttemptEvent args)
    {
        if (args.Solution.Name != BloodstreamComponent.DefaultBloodSolutionName
            && args.Solution.Name != BloodstreamComponent.DefaultChemicalsSolutionName
            && args.Solution.Name != BloodstreamComponent.DefaultBloodTemporarySolutionName)
            return;

        foreach (var effect in args.Reaction.Effects)
        {
            switch (effect)
            {
                case CreateEntityReactionEffect: // Prevent entities from spawning in the bloodstream
                case AreaReactionEffect: // No spontaneous smoke or foam leaking out of blood vessels.
                    args.Cancel();
                    return;
            }
        }

        // The area-reaction effect canceling is part of avoiding smoke-fork-bombs (create two smoke bombs, that when
        // ingested by mobs create more smoke). This also used to act as a rapid chemical-purge, because all the
        // reagents would get carried away by the smoke/foam. This does still work for the stomach (I guess people vomit
        // up the smoke or spawned entities?).

        // TODO apply organ damage instead of just blocking the reaction?
        // Having cheese-clots form in your veins can't be good for you.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var bloodstream in EntityManager.EntityQuery<BloodstreamComponent>())
        {
            bloodstream.AccumulatedFrametime += frameTime;

            if (bloodstream.AccumulatedFrametime < bloodstream.UpdateInterval)
                continue;

            bloodstream.AccumulatedFrametime -= bloodstream.UpdateInterval;

            var uid = bloodstream.Owner;
            if (TryComp<MobStateComponent>(uid, out var state) && _mobStateSystem.IsDead(uid, state))
                continue;

            // First, let's refresh their blood if possible.
            if (bloodstream.BloodSolution.CurrentVolume < bloodstream.BloodSolution.MaxVolume)
                TryModifyBloodLevel(uid, bloodstream.BloodRefreshAmount, bloodstream);

            // Next, let's remove some blood from them according to their bleed level.
            // as well as stop their bleeding to a certain extent.
            if (bloodstream.BleedAmount > 0)
            {
                TryModifyBloodLevel(uid, (-bloodstream.BleedAmount) / 20, bloodstream);
                TryModifyBleedAmount(uid, -bloodstream.BleedReductionAmount, bloodstream);
            }

            // Next, we'll deal some bloodloss damage if their blood level is below a threshold.
            var bloodPercentage = GetBloodLevelPercentage(uid, bloodstream);
            if (bloodPercentage < bloodstream.BloodlossThreshold)
            {
                // TODO use a better method for determining this.
                var amt = bloodstream.BloodlossDamage / (0.1f + bloodPercentage);

                _damageableSystem.TryChangeDamage(uid, amt, true, false);

                // Apply dizziness as a symptom of bloodloss.
                // So, threshold is 0.9, you have 0.85 percent blood, it adds (5 * 1.05) or 5.25 seconds of drunkenness.
                // So, it'd max at 1.9 by default with 0% blood.
                _drunkSystem.TryApplyDrunkenness(uid, bloodstream.UpdateInterval * (1 + (bloodstream.BloodlossThreshold - bloodPercentage)), false);
            }
            else
            {
                // If they're healthy, we'll try and heal some bloodloss instead.
                _damageableSystem.TryChangeDamage(uid, bloodstream.BloodlossHealDamage * bloodPercentage, true, false);
            }
        }
    }

    private void OnComponentInit(EntityUid uid, BloodstreamComponent component, ComponentInit args)
    {
        component.ChemicalSolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultChemicalsSolutionName);
        component.BloodSolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultBloodSolutionName);
        component.BloodTemporarySolution = _solutionContainerSystem.EnsureSolution(uid, BloodstreamComponent.DefaultBloodTemporarySolutionName);

        component.ChemicalSolution.MaxVolume = component.ChemicalMaxVolume;
        component.BloodSolution.MaxVolume = component.BloodMaxVolume;
        component.BloodTemporarySolution.MaxVolume = component.BleedPuddleThreshold * 4; // give some leeway, for chemstream as well

        // Fill blood solution with BLOOD
        _solutionContainerSystem.TryAddReagent(uid, component.BloodSolution, component.BloodReagent,
            component.BloodMaxVolume, out _);
    }

    private void OnDamageChanged(EntityUid uid, BloodstreamComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        // TODO probably cache this or something. humans get hurt a lot
        if (!_prototypeManager.TryIndex<DamageModifierSetPrototype>(component.DamageBleedModifiers, out var modifiers))
            return;

        var bloodloss = DamageSpecifier.ApplyModifierSet(args.DamageDelta, modifiers);

        if (bloodloss.Empty)
            return;

        var oldBleedAmount = component.BleedAmount;
        var total = bloodloss.Total;
        var totalFloat = total.Float();
        TryModifyBleedAmount(uid, totalFloat, component);

        var prob = Math.Clamp(totalFloat / 50, 0, 1);
        var healPopupProb = Math.Clamp(Math.Abs(totalFloat) / 25, 0, 1);
        if (totalFloat > 0 && _robustRandom.Prob(prob))
        {
            TryModifyBloodLevel(uid, (-total) / 5, component);
            SoundSystem.Play(component.InstantBloodSound.GetSound(), Filter.Pvs(uid), uid, AudioParams.Default);
        }
        else if (totalFloat < 0 && oldBleedAmount > 0 && _robustRandom.Prob(healPopupProb))
        {
            // Magically, this damage has healed some bleeding, likely
            // because it's burn damage that cauterized their wounds.

            // We'll play a special sound and popup for feedback.
            SoundSystem.Play(component.BloodHealedSound.GetSound(), Filter.Pvs(uid), uid, AudioParams.Default);
            _popupSystem.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"), uid,
                uid, PopupType.Medium);
;       }
    }

    private void OnHealthBeingExamined(EntityUid uid, BloodstreamComponent component, HealthBeingExaminedEvent args)
    {
        if (component.BleedAmount > 10)
        {
            args.Message.PushNewline();
            args.Message.AddMarkup(Loc.GetString("bloodstream-component-profusely-bleeding", ("target", Identity.Entity(uid, EntityManager))));
        }
        else if (component.BleedAmount > 0)
        {
            args.Message.PushNewline();
            args.Message.AddMarkup(Loc.GetString("bloodstream-component-bleeding", ("target", Identity.Entity(uid, EntityManager))));
        }

        if (GetBloodLevelPercentage(uid, component) < component.BloodlossThreshold)
        {
            args.Message.PushNewline();
            args.Message.AddMarkup(Loc.GetString("bloodstream-component-looks-pale", ("target", Identity.Entity(uid, EntityManager))));
        }
    }

    private void OnBeingGibbed(EntityUid uid, BloodstreamComponent component, BeingGibbedEvent args)
    {
        SpillAllSolutions(uid, component);
    }

    private void OnApplyMetabolicMultiplier(EntityUid uid, BloodstreamComponent component, ApplyMetabolicMultiplierEvent args)
    {
        if (args.Apply)
        {
            component.UpdateInterval *= args.Multiplier;
            return;
        }
        component.UpdateInterval /= args.Multiplier;
        // Reset the accumulator properly
        if (component.AccumulatedFrametime >= component.UpdateInterval)
            component.AccumulatedFrametime = component.UpdateInterval;
    }

    private void OnRejuvenate(EntityUid uid, BloodstreamComponent component, RejuvenateEvent args)
    {
        TryModifyBleedAmount(uid, -component.BleedAmount, component);
        TryModifyBloodLevel(uid, component.BloodSolution.AvailableVolume, component);
    }

    /// <summary>
    ///     Attempt to transfer provided solution to internal solution.
    /// </summary>
    public bool TryAddToChemicals(EntityUid uid, Solution solution, BloodstreamComponent? component=null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return _solutionContainerSystem.TryAddSolution(uid, component.ChemicalSolution, solution);
    }

    public bool FlushChemicals(EntityUid uid, string excludedReagentID, FixedPoint2 quantity, BloodstreamComponent? component = null) {
        if (!Resolve(uid, ref component, false))
            return false;

        for (var i = component.ChemicalSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = component.ChemicalSolution.Contents[i];
            if (reagentId != excludedReagentID)
            {
                _solutionContainerSystem.TryRemoveReagent(uid, component.ChemicalSolution, reagentId, quantity);
            }
        }

        return true;
    }

    public float GetBloodLevelPercentage(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0.0f;

        return (component.BloodSolution.CurrentVolume / component.BloodSolution.MaxVolume).Float();
    }

    public void SetBloodLossThreshold(EntityUid uid, float threshold, BloodstreamComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.BloodlossThreshold = threshold;
    }

    /// <summary>
    ///     Attempts to modify the blood level of this entity directly.
    /// </summary>
    public bool TryModifyBloodLevel(EntityUid uid, FixedPoint2 amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (amount >= 0)
            return _solutionContainerSystem.TryAddReagent(uid, component.BloodSolution, component.BloodReagent, amount, out _);

        // Removal is more involved,
        // since we also wanna handle moving it to the temporary solution
        // and then spilling it if necessary.
        var newSol = component.BloodSolution.SplitSolution(-amount);
        component.BloodTemporarySolution.AddSolution(newSol);

        if (component.BloodTemporarySolution.CurrentVolume > component.BleedPuddleThreshold)
        {
            // Pass some of the chemstream into the spilled blood.
            var temp = component.ChemicalSolution.SplitSolution(component.BloodTemporarySolution.CurrentVolume / 10);
            component.BloodTemporarySolution.AddSolution(temp);
            _spillableSystem.SpillAt(uid, component.BloodTemporarySolution, "PuddleBlood", false);
            component.BloodTemporarySolution.RemoveAllSolution();
        }

        return true;
    }

    /// <summary>
    ///     Tries to make an entity bleed more or less
    /// </summary>
    public bool TryModifyBleedAmount(EntityUid uid, float amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        component.BleedAmount += amount;
        component.BleedAmount = Math.Clamp(component.BleedAmount, 0, component.MaxBleedAmount);

        return true;
    }

    /// <summary>
    ///     BLOOD FOR THE BLOOD GOD
    /// </summary>
    public void SpillAllSolutions(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var max = component.BloodSolution.MaxVolume + component.BloodTemporarySolution.MaxVolume +
                  component.ChemicalSolution.MaxVolume;
        var tempSol = new Solution() { MaxVolume = max };

        tempSol.AddSolution(component.BloodSolution);
        component.BloodSolution.RemoveAllSolution();
        tempSol.AddSolution(component.BloodTemporarySolution);
        component.BloodTemporarySolution.RemoveAllSolution();
        tempSol.AddSolution(component.ChemicalSolution);
        component.ChemicalSolution.RemoveAllSolution();
        _spillableSystem.SpillAt(uid, tempSol, "PuddleBlood", true);
    }
}
