using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.HealthExaminable;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Rounding;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public abstract class SharedBloodstreamSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainerSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodstreamComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
        SubscribeLocalEvent<BloodstreamComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BloodstreamComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BloodstreamComponent, GenerateDnaEvent>(OnDnaGenerated);
    }

    private void OnMapInit(Entity<BloodstreamComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = GameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnReactionAttempt(Entity<BloodstreamComponent> entity, ref ReactionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var effect in args.Reaction.Effects)
        {
            switch (effect)
            {
                case CreateEntityReactionEffect: // Prevent entities from spawning in the bloodstream
                case AreaReactionEffect: // No spontaneous smoke or foam leaking out of blood vessels.
                    args.Cancelled = true;
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

    private void OnReactionAttempt(Entity<BloodstreamComponent> entity, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (args.Name != entity.Comp.BloodSolutionName
            && args.Name != entity.Comp.ChemicalSolutionName
            && args.Name != entity.Comp.BloodTemporarySolutionName)
        {
            return;
        }

        OnReactionAttempt(entity, ref args.Event);
    }

    private void OnComponentInit(Entity<BloodstreamComponent> entity, ref ComponentInit args)
    {
        if (!SolutionContainerSystem.EnsureSolution(entity.Owner,
                entity.Comp.ChemicalSolutionName,
                out var chemicalSolution) ||
            !SolutionContainerSystem.EnsureSolution(entity.Owner,
                entity.Comp.BloodSolutionName,
                out var bloodSolution) ||
            !SolutionContainerSystem.EnsureSolution(entity.Owner,
                entity.Comp.BloodTemporarySolutionName,
                out var tempSolution))
            return;

        chemicalSolution.MaxVolume = entity.Comp.ChemicalMaxVolume;
        bloodSolution.MaxVolume = entity.Comp.BloodMaxVolume;
        tempSolution.MaxVolume = entity.Comp.BleedPuddleThreshold * 4; // give some leeway, for chemstream as well

        // Fill blood solution with BLOOD
        // The DNA string might not be initialized yet, but the reagent data gets updated in the GenerateDnaEvent subscription
        bloodSolution.AddReagent(new ReagentId(entity.Comp.BloodReagent, GetEntityBloodData(entity.Owner)), entity.Comp.BloodMaxVolume - bloodSolution.Volume);
        SetBleedAlert(entity!);
    }

    private void OnDamageChanged(Entity<BloodstreamComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null || !args.DamageIncreased || GameTiming.ApplyingState)
            return;

        // TODO probably cache this or something. humans get hurt a lot
        if (!_prototypeManager.TryIndex(ent.Comp.DamageBleedModifiers, out var modifiers))
            return;

        // some reagents may deal and heal different damage types in the same tick, which means DamageIncreased will be true
        // but we only want to consider the dealt damage when causing bleeding
        var damage = DamageSpecifier.GetPositive(args.DamageDelta);
        var bloodloss = DamageSpecifier.ApplyModifierSet(damage, modifiers);

        if (bloodloss.Empty)
            return;

        // Does the calculation of how much bleed rate should be added/removed, then applies it
        var oldBleedAmount = ent.Comp.BleedAmount;
        var total = bloodloss.GetTotal();
        var totalFloat = total.Float();
        TryModifyBleedAmount(ent!, totalFloat);

        /*
        ///     Critical hit. Causes target to lose blood, using the bleed rate modifier of the weapon, currently divided by 5
        ///     The crit chance is currently the bleed rate modifier divided by 25.
        ///     Higher damage weapons have a higher chance to crit!
        */
        var prob = Math.Clamp(totalFloat / 25, 0, 1);
        if (totalFloat > 0 && _robustRandom.Prob(prob))
        {
            TryModifyBloodLevel(ent, -total / 5, ent);
            _audio.PlayPredicted(ent.Comp.InstantBloodSound, ent, ent);
        }

        // Heat damage will cauterize, causing the bleed rate to be reduced.
        else if (totalFloat <= ent.Comp.BloodHealedSoundThreshold && oldBleedAmount > 0)
        {
            // Magically, this damage has healed some bleeding, likely
            // because it's burn damage that cauterized their wounds.

            // We'll play a special sound and popup for feedback.
            _audio.PlayPredicted(ent.Comp.BloodHealedSound, ent, ent);
            _popupSystem.PopupPredicted(Loc.GetString("bloodstream-component-wounds-cauterized"), ent, ent, PopupType.Medium);
        }

        Dirty(ent);
    }
    /// <summary>
    ///     Shows text on health examine, based on bleed rate and blood level.
    /// </summary>
    private void OnHealthBeingExamined(Entity<BloodstreamComponent> ent, ref HealthBeingExaminedEvent args)
    {
        // Shows massively bleeding at 0.75x the max bleed rate.
        if (ent.Comp.BleedAmount > ent.Comp.MaxBleedAmount * 0.75f)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-massive-bleeding", ("target", ent.Owner)));
        }
        // Shows bleeding message when bleeding above half the max rate, but less than massively.
        else if (ent.Comp.BleedAmount > ent.Comp.MaxBleedAmount * 0.5f)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-strong-bleeding", ("target", ent.Owner)));
        }
        // Shows bleeding message when bleeding above 0.25x the max rate, but less than half the max.
        else if (ent.Comp.BleedAmount > ent.Comp.MaxBleedAmount * 0.25f)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-bleeding", ("target", ent.Owner)));
        }
        // Shows bleeding message when bleeding below 0.25x the max cap
        else if (ent.Comp.BleedAmount > 0)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-slight-bleeding", ("target", ent.Owner)));
        }

        // If the mob's blood level is below the damage threshhold, the pale message is added.
        if (GetBloodLevelPercentage(ent, ent) < ent.Comp.BloodlossThreshold)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-looks-pale", ("target", ent.Owner)));
        }
    }

    private void OnApplyMetabolicMultiplier(
        Entity<BloodstreamComponent> ent,
        ref ApplyMetabolicMultiplierEvent args)
    {
        // TODO REFACTOR THIS
        // This will slowly drift over time due to floating point errors.
        // Instead, raise an event with the base rates and allow modifiers to get applied to it.
        if (args.Apply)
        {
            ent.Comp.UpdateInterval *= args.Multiplier;
            return;
        }
        ent.Comp.UpdateInterval /= args.Multiplier;
    }

    private void OnRejuvenate(Entity<BloodstreamComponent> entity, ref RejuvenateEvent args)
    {
        TryModifyBleedAmount(entity!, -entity.Comp.BleedAmount);

        if (SolutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
            TryModifyBloodLevel(entity.Owner, bloodSolution.AvailableVolume, entity.Comp);

        if (SolutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.ChemicalSolutionName, ref entity.Comp.ChemicalSolution))
            SolutionContainerSystem.RemoveAllSolution(entity.Comp.ChemicalSolution.Value);

        Dirty(entity);
    }

    /// <summary>
    ///     Attempt to transfer provided solution to internal solution.
    /// </summary>
    public bool TryAddToChemicals(EntityUid uid, Solution solution, BloodstreamComponent? component = null)
    {
        return Resolve(uid, ref component, logMissing: false)
            && SolutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution)
            && SolutionContainerSystem.TryAddSolution(component.ChemicalSolution.Value, solution);
    }

    public bool FlushChemicals(EntityUid uid, string excludedReagentId, FixedPoint2 quantity, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, logMissing: false)
            || !SolutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution, out var chemSolution))
            return false;

        for (var i = chemSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = chemSolution.Contents[i];
            if (reagentId.Prototype != excludedReagentId)
            {
                SolutionContainerSystem.RemoveReagent(component.ChemicalSolution.Value, reagentId, quantity);
            }
        }

        return true;
    }

    public float GetBloodLevelPercentage(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component)
            || !SolutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution, out var bloodSolution))
        {
            return 0.0f;
        }

        return bloodSolution.FillFraction;
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
        if (!Resolve(uid, ref component, logMissing: false)
            || !SolutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution))
        {
            return false;
        }

        if (amount >= 0)
            return SolutionContainerSystem.TryAddReagent(component.BloodSolution.Value, component.BloodReagent, amount, null, GetEntityBloodData(uid));

        // Removal is more involved,
        // since we also wanna handle moving it to the temporary solution
        // and then spilling it if necessary.
        var newSol = SolutionContainerSystem.SplitSolution(component.BloodSolution.Value, -amount);

        if (!SolutionContainerSystem.ResolveSolution(uid, component.BloodTemporarySolutionName, ref component.TemporarySolution, out var tempSolution))
            return true;

        tempSolution.AddSolution(newSol, _prototypeManager);

        if (tempSolution.Volume > component.BleedPuddleThreshold)
        {
            // Pass some of the chemstream into the spilled blood.
            if (SolutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution))
            {
                var temp = SolutionContainerSystem.SplitSolution(component.ChemicalSolution.Value, tempSolution.Volume / 10);
                tempSolution.AddSolution(temp, _prototypeManager);
            }

            _puddleSystem.TrySpillAt(uid, tempSolution, out _, sound: false);

            tempSolution.RemoveAllSolution();
        }

        SolutionContainerSystem.UpdateChemicals(component.TemporarySolution.Value);

        return true;
    }

    /// <summary>
    ///     Tries to make an entity bleed more or less
    /// </summary>
    public bool TryModifyBleedAmount(Entity<BloodstreamComponent?> entity, FixedPoint2 amount)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        entity.Comp.BleedAmount += amount;
        entity.Comp.BleedAmount = Math.Clamp((float)entity.Comp.BleedAmount, 0f, (float)entity.Comp.MaxBleedAmount);

        SetBleedAlert(entity);

        return true;
    }

    private void SetBleedAlert(Entity<BloodstreamComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        if (entity.Comp.BleedAmount == 0)
            _alertsSystem.ClearAlert(entity, entity.Comp.BleedingAlert);
        else
        {
            var severity = (short) ContentHelpers.RoundToLevels((float)entity.Comp.BleedAmount, (float)entity.Comp.MaxBleedAmount, 10);
            _alertsSystem.ShowAlert(entity, entity.Comp.BleedingAlert, severity);
        }
    }

    /// <summary>
    ///     BLOOD FOR THE BLOOD GOD
    /// </summary>
    public void SpillAllSolutions(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var tempSol = new Solution();

        if (SolutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution, out var bloodSolution))
        {
            tempSol.MaxVolume += bloodSolution.MaxVolume;
            tempSol.AddSolution(bloodSolution, _prototypeManager);
            SolutionContainerSystem.RemoveAllSolution(component.BloodSolution.Value);
        }

        if (SolutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution, out var chemSolution))
        {
            tempSol.MaxVolume += chemSolution.MaxVolume;
            tempSol.AddSolution(chemSolution, _prototypeManager);
            SolutionContainerSystem.RemoveAllSolution(component.ChemicalSolution.Value);
        }

        if (SolutionContainerSystem.ResolveSolution(uid, component.BloodTemporarySolutionName, ref component.TemporarySolution, out var tempSolution))
        {
            tempSol.MaxVolume += tempSolution.MaxVolume;
            tempSol.AddSolution(tempSolution, _prototypeManager);
            SolutionContainerSystem.RemoveAllSolution(component.TemporarySolution.Value);
        }

        _puddleSystem.TrySpillAt(uid, tempSol, out _);
    }

    /// <summary>
    ///     Change what someone's blood is made of, on the fly.
    /// </summary>
    public void ChangeBloodReagent(EntityUid uid, string reagent, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, logMissing: false)
            || reagent == component.BloodReagent)
        {
            return;
        }

        if (!SolutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution, out var bloodSolution))
        {
            component.BloodReagent = reagent;
            return;
        }

        var currentVolume = bloodSolution.RemoveReagent(component.BloodReagent, bloodSolution.Volume, ignoreReagentData: true);

        component.BloodReagent = reagent;

        if (currentVolume > 0)
            SolutionContainerSystem.TryAddReagent(component.BloodSolution.Value, component.BloodReagent, currentVolume, null, GetEntityBloodData(uid));
    }

    private void OnDnaGenerated(Entity<BloodstreamComponent> entity, ref GenerateDnaEvent args)
    {
        if (SolutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
        {
            foreach (var reagent in bloodSolution.Contents)
            {
                List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                reagentData.RemoveAll(x => x is DnaData);
                reagentData.AddRange(GetEntityBloodData(entity.Owner));
            }
        }
        else
            Log.Error("Unable to set bloodstream DNA, solution entity could not be resolved");
    }

    /// <summary>
    /// Get the reagent data for blood that a specific entity should have.
    /// </summary>
    public List<ReagentData> GetEntityBloodData(EntityUid uid)
    {
        var bloodData = new List<ReagentData>();
        var dnaData = new DnaData();

        if (TryComp<DnaComponent>(uid, out var donorComp) && donorComp.DNA != null)
            dnaData.DNA = donorComp.DNA;
        else
            dnaData.DNA = Loc.GetString("forensics-dna-unknown");

        bloodData.Add(dnaData);

        return bloodData;
    }
}
