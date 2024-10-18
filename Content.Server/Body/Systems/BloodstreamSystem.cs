using Content.Server.Body.Components;
using Content.Server.EntityEffects.Effects;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics;
using Content.Shared.HealthExaminable;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech.EntitySystems;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedStutteringSystem _stutteringSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodstreamComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodstreamComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
        SubscribeLocalEvent<BloodstreamComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<BloodstreamComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BloodstreamComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BloodstreamComponent, GenerateDnaEvent>(OnDnaGenerated);
    }

    private void OnMapInit(Entity<BloodstreamComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnUnpaused(Entity<BloodstreamComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextUpdate += args.PausedTime;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (_gameTiming.CurTime < bloodstream.NextUpdate)
                continue;

            bloodstream.NextUpdate += bloodstream.UpdateInterval;

            if (!_solutionContainerSystem.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                continue;

            // Adds blood to their blood level if it is below the maximum; Blood regeneration. Must be alive.
            if (bloodSolution.Volume < bloodSolution.MaxVolume && !_mobStateSystem.IsDead(uid))
            {
                TryModifyBloodLevel(uid, bloodstream.BloodRefreshAmount, bloodstream);
            }

            // Removes blood from the bloodstream based on bleed amount (bleed rate)
            // as well as stop their bleeding to a certain extent.
            if (bloodstream.BleedAmount > 0)
            {
                // Blood is removed from the bloodstream at a 1-1 rate with the bleed amount
                TryModifyBloodLevel(uid, (-bloodstream.BleedAmount), bloodstream);
                // Bleed rate is reduced by the bleed reduction amount in the bloodstream component.
                TryModifyBleedAmount(uid, -bloodstream.BleedReductionAmount, bloodstream);
            }

            // deal bloodloss damage if their blood level is below a threshold.
            var bloodPercentage = GetBloodLevelPercentage(uid, bloodstream);
            if (bloodPercentage < bloodstream.BloodlossThreshold && !_mobStateSystem.IsDead(uid))
            {
                // bloodloss damage is based on the base value, and modified by how low your blood level is.
                var amt = bloodstream.BloodlossDamage / (0.1f + bloodPercentage);

                _damageableSystem.TryChangeDamage(uid, amt,
                    ignoreResistances: false, interruptsDoAfters: false);

                // Apply dizziness as a symptom of bloodloss.
                // The effect is applied in a way that it will never be cleared without being healthy.
                // Multiplying by 2 is arbitrary but works for this case, it just prevents the time from running out
                _drunkSystem.TryApplyDrunkenness(
                    uid,
                    (float) bloodstream.UpdateInterval.TotalSeconds * 2,
                    applySlur: false);
                _stutteringSystem.DoStutter(uid, bloodstream.UpdateInterval * 2, refresh: false);

                // storing the drunk and stutter time so we can remove it independently from other effects additions
                bloodstream.StatusTime += bloodstream.UpdateInterval * 2;
            }
            else if (!_mobStateSystem.IsDead(uid))
            {
                // If they're healthy, we'll try and heal some bloodloss instead.
                _damageableSystem.TryChangeDamage(
                    uid,
                    bloodstream.BloodlossHealDamage * bloodPercentage,
                    ignoreResistances: true, interruptsDoAfters: false);

                // Remove the drunk effect when healthy. Should only remove the amount of drunk and stutter added by low blood level
                _drunkSystem.TryRemoveDrunkenessTime(uid, bloodstream.StatusTime.TotalSeconds);
                _stutteringSystem.DoRemoveStutterTime(uid, bloodstream.StatusTime.TotalSeconds);
                // Reset the drunk and stutter time to zero
                bloodstream.StatusTime = TimeSpan.Zero;
            }
        }
    }

    private void OnComponentInit(Entity<BloodstreamComponent> entity, ref ComponentInit args)
    {
        if (!_solutionContainerSystem.EnsureSolution(entity.Owner,
                entity.Comp.ChemicalSolutionName,
                out var chemicalSolution) ||
            !_solutionContainerSystem.EnsureSolution(entity.Owner,
                entity.Comp.BloodSolutionName,
                out var bloodSolution) ||
            !_solutionContainerSystem.EnsureSolution(entity.Owner,
                entity.Comp.BloodTemporarySolutionName,
                out var tempSolution))
            return;

        chemicalSolution.MaxVolume = entity.Comp.ChemicalMaxVolume;
        bloodSolution.MaxVolume = entity.Comp.BloodMaxVolume;
        tempSolution.MaxVolume = entity.Comp.BleedPuddleThreshold * 4; // give some leeway, for chemstream as well

        // Ensure blood that should have DNA has it; must be run here, in case DnaComponent has not yet been initialized

        if (TryComp<DnaComponent>(entity.Owner, out var donorComp) && donorComp.DNA == String.Empty)
        {
            donorComp.DNA = _forensicsSystem.GenerateDNA();

            var ev = new GenerateDnaEvent { Owner = entity.Owner, DNA = donorComp.DNA };
            RaiseLocalEvent(entity.Owner, ref ev);
        }

        // Fill blood solution with BLOOD
        bloodSolution.AddReagent(new ReagentId(entity.Comp.BloodReagent, GetEntityBloodData(entity.Owner)), entity.Comp.BloodMaxVolume - bloodSolution.Volume);
    }

    private void OnDamageChanged(Entity<BloodstreamComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null || !args.DamageIncreased)
        {
            return;
        }

        // TODO probably cache this or something. humans get hurt a lot
        if (!_prototypeManager.TryIndex<DamageModifierSetPrototype>(ent.Comp.DamageBleedModifiers, out var modifiers))
            return;

        var bloodloss = DamageSpecifier.ApplyModifierSet(args.DamageDelta, modifiers);

        if (bloodloss.Empty)
            return;

        // Does the calculation of how much bleed rate should be added/removed, then applies it
        var oldBleedAmount = ent.Comp.BleedAmount;
        var total = bloodloss.GetTotal();
        var totalFloat = total.Float();
        TryModifyBleedAmount(ent, totalFloat, ent);

        /// <summary>
        ///     Critical hit. Causes target to lose blood, using the bleed rate modifier of the weapon, currently divided by 5
        ///     The crit chance is currently the bleed rate modifier divided by 25.
        ///     Higher damage weapons have a higher chance to crit!
        /// </summary>
        var prob = Math.Clamp(totalFloat / 25, 0, 1);
        if (totalFloat > 0 && _robustRandom.Prob(prob))
        {
            TryModifyBloodLevel(ent, (-total) / 5, ent);
            _audio.PlayPvs(ent.Comp.InstantBloodSound, ent);
        }

        // Heat damage will cauterize, causing the bleed rate to be reduced.
        else if (totalFloat <= ent.Comp.BloodHealedSoundThreshold && oldBleedAmount > 0)
        {
            // Magically, this damage has healed some bleeding, likely
            // because it's burn damage that cauterized their wounds.

            // We'll play a special sound and popup for feedback.
            _audio.PlayPvs(ent.Comp.BloodHealedSound, ent);
            _popupSystem.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"), ent,
                ent, PopupType.Medium);
        }
    }
    /// <summary>
    ///     Shows text on health examine, based on bleed rate and blood level.
    /// </summary>
    private void OnHealthBeingExamined(Entity<BloodstreamComponent> ent, ref HealthBeingExaminedEvent args)
    {
        // Shows profusely bleeding at half the max bleed rate.
        if (ent.Comp.BleedAmount > ent.Comp.MaxBleedAmount / 2)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-profusely-bleeding", ("target", ent.Owner)));
        }
        // Shows bleeding message when bleeding, but less than profusely.
        else if (ent.Comp.BleedAmount > 0)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-bleeding", ("target", ent.Owner)));
        }

        // If the mob's blood level is below the damage threshhold, the pale message is added.
        if (GetBloodLevelPercentage(ent, ent) < ent.Comp.BloodlossThreshold)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-looks-pale", ("target", ent.Owner)));
        }
    }

    private void OnBeingGibbed(Entity<BloodstreamComponent> ent, ref BeingGibbedEvent args)
    {
        SpillAllSolutions(ent, ent);
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
        TryModifyBleedAmount(entity.Owner, -entity.Comp.BleedAmount, entity.Comp);

        if (_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
            TryModifyBloodLevel(entity.Owner, bloodSolution.AvailableVolume, entity.Comp);

        if (_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.ChemicalSolutionName, ref entity.Comp.ChemicalSolution))
            _solutionContainerSystem.RemoveAllSolution(entity.Comp.ChemicalSolution.Value);
    }

    /// <summary>
    ///     Attempt to transfer provided solution to internal solution.
    /// </summary>
    public bool TryAddToChemicals(EntityUid uid, Solution solution, BloodstreamComponent? component = null)
    {
        return Resolve(uid, ref component, logMissing: false)
            && _solutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution)
            && _solutionContainerSystem.TryAddSolution(component.ChemicalSolution.Value, solution);
    }

    public bool FlushChemicals(EntityUid uid, string excludedReagentID, FixedPoint2 quantity, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution, out var chemSolution))
            return false;

        for (var i = chemSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = chemSolution.Contents[i];
            if (reagentId.Prototype != excludedReagentID)
            {
                _solutionContainerSystem.RemoveReagent(component.ChemicalSolution.Value, reagentId, quantity);
            }
        }

        return true;
    }

    public float GetBloodLevelPercentage(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component)
            || !_solutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution, out var bloodSolution))
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
            || !_solutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution))
        {
            return false;
        }

        if (amount >= 0)
            return _solutionContainerSystem.TryAddReagent(component.BloodSolution.Value, component.BloodReagent, amount, null, GetEntityBloodData(uid));

        // Removal is more involved,
        // since we also wanna handle moving it to the temporary solution
        // and then spilling it if necessary.
        var newSol = _solutionContainerSystem.SplitSolution(component.BloodSolution.Value, -amount);

        if (!_solutionContainerSystem.ResolveSolution(uid, component.BloodTemporarySolutionName, ref component.TemporarySolution, out var tempSolution))
            return true;

        tempSolution.AddSolution(newSol, _prototypeManager);

        if (tempSolution.Volume > component.BleedPuddleThreshold)
        {
            // Pass some of the chemstream into the spilled blood.
            if (_solutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution))
            {
                var temp = _solutionContainerSystem.SplitSolution(component.ChemicalSolution.Value, tempSolution.Volume / 10);
                tempSolution.AddSolution(temp, _prototypeManager);
            }

            _puddleSystem.TrySpillAt(uid, tempSolution, out var puddleUid, sound: false);

            tempSolution.RemoveAllSolution();
        }

        _solutionContainerSystem.UpdateChemicals(component.TemporarySolution.Value);

        return true;
    }

    /// <summary>
    ///     Tries to make an entity bleed more or less
    /// </summary>
    public bool TryModifyBleedAmount(EntityUid uid, float amount, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component, logMissing: false))
            return false;

        component.BleedAmount += amount;
        component.BleedAmount = Math.Clamp(component.BleedAmount, 0, component.MaxBleedAmount);

        if (component.BleedAmount == 0)
            _alertsSystem.ClearAlert(uid, component.BleedingAlert);
        else
        {
            var severity = (short) Math.Clamp(Math.Round(component.BleedAmount, MidpointRounding.ToZero), 0, 10);
            _alertsSystem.ShowAlert(uid, component.BleedingAlert, severity);
        }

        return true;
    }

    /// <summary>
    ///     BLOOD FOR THE BLOOD GOD
    /// </summary>
    public void SpillAllSolutions(EntityUid uid, BloodstreamComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var tempSol = new Solution();

        if (_solutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution, out var bloodSolution))
        {
            tempSol.MaxVolume += bloodSolution.MaxVolume;
            tempSol.AddSolution(bloodSolution, _prototypeManager);
            _solutionContainerSystem.RemoveAllSolution(component.BloodSolution.Value);
        }

        if (_solutionContainerSystem.ResolveSolution(uid, component.ChemicalSolutionName, ref component.ChemicalSolution, out var chemSolution))
        {
            tempSol.MaxVolume += chemSolution.MaxVolume;
            tempSol.AddSolution(chemSolution, _prototypeManager);
            _solutionContainerSystem.RemoveAllSolution(component.ChemicalSolution.Value);
        }

        if (_solutionContainerSystem.ResolveSolution(uid, component.BloodTemporarySolutionName, ref component.TemporarySolution, out var tempSolution))
        {
            tempSol.MaxVolume += tempSolution.MaxVolume;
            tempSol.AddSolution(tempSolution, _prototypeManager);
            _solutionContainerSystem.RemoveAllSolution(component.TemporarySolution.Value);
        }

        _puddleSystem.TrySpillAt(uid, tempSol, out var puddleUid);
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

        if (!_solutionContainerSystem.ResolveSolution(uid, component.BloodSolutionName, ref component.BloodSolution, out var bloodSolution))
        {
            component.BloodReagent = reagent;
            return;
        }

        var currentVolume = bloodSolution.RemoveReagent(component.BloodReagent, bloodSolution.Volume);

        component.BloodReagent = reagent;

        if (currentVolume > 0)
            _solutionContainerSystem.TryAddReagent(component.BloodSolution.Value, component.BloodReagent, currentVolume, null, GetEntityBloodData(uid));
    }

    private void OnDnaGenerated(Entity<BloodstreamComponent> entity, ref GenerateDnaEvent args)
    {
        if (_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
        {
            foreach (var reagent in bloodSolution.Contents)
            {
                List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                reagentData.RemoveAll(x => x is DnaData);
                reagentData.AddRange(GetEntityBloodData(entity.Owner));
            }
        }
    }

    /// <summary>
    /// Get the reagent data for blood that a specific entity should have.
    /// </summary>
    public List<ReagentData> GetEntityBloodData(EntityUid uid)
    {
        var bloodData = new List<ReagentData>();
        var dnaData = new DnaData();

        if (TryComp<DnaComponent>(uid, out var donorComp))
        {
            dnaData.DNA = donorComp.DNA;
        } else
        {
            dnaData.DNA = Loc.GetString("forensics-dna-unknown");
        }

        bloodData.Add(dnaData);

        return bloodData;
    }
}
