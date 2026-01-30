using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects.Effects.Solution;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.Gibbing;
using Content.Shared.HealthExaminable;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public abstract class SharedBloodstreamSystem : EntitySystem
{
    public static readonly EntProtoId Bloodloss = "StatusEffectBloodloss";

    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodstreamComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BloodstreamComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);
        SubscribeLocalEvent<BloodstreamComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BloodstreamComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
        SubscribeLocalEvent<BloodstreamComponent, GibbedBeforeDeletionEvent>(OnBeingGibbed);
        SubscribeLocalEvent<BloodstreamComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<BloodstreamComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BloodstreamComponent, MetabolismExclusionEvent>(OnMetabolismExclusion);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            if (curTime < bloodstream.NextUpdate)
                continue;

            bloodstream.NextUpdate += bloodstream.AdjustedUpdateInterval;
            DirtyField(uid, bloodstream, nameof(BloodstreamComponent.NextUpdate)); // needs to be dirtied on the client so it can be rerolled during prediction

            if (!SolutionContainer.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
                continue;

            // Blood level regulation. Must be alive.
            if (!_mobStateSystem.IsDead(uid))
            {
                TryRegulateBloodLevel(uid, bloodstream.BloodRefreshAmount);

                TickBleed((uid, bloodstream));

                // deal bloodloss damage if their blood level is below a threshold.
                var bloodPercentage = GetBloodLevel(uid);
                if (bloodPercentage < bloodstream.BloodlossThreshold)
                {
                    // bloodloss damage is based on the base value, and modified by how low your blood level is.
                    var amt = bloodstream.BloodlossDamage / (0.1f + bloodPercentage);

                    _damageableSystem.TryChangeDamage(uid, amt, ignoreResistances: false, interruptsDoAfters: false);

                    // Apply dizziness as a symptom of bloodloss.
                    // The effect is applied in a way that it will never be cleared without being healthy.
                    // Multiplying by 2 is arbitrary but works for this case, it just prevents the time from running out
                    _status.TrySetStatusEffectDuration(uid, Bloodloss);
                }
                else
                {
                    // If they're healthy, we'll try and heal some bloodloss instead.
                    _damageableSystem.TryChangeDamage(uid, bloodstream.BloodlossHealDamage * bloodPercentage, ignoreResistances: true, interruptsDoAfters: false);

                    _status.TryRemoveStatusEffect(uid, Bloodloss);
                }
            }
            else
            {
                TickBleed((uid, bloodstream));
            }
        }
    }

    private void OnMapInit(Entity<BloodstreamComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.AdjustedUpdateInterval;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.NextUpdate));
    }

    // prevent the infamous UdderSystem debug assert, see https://github.com/space-wizards/space-station-14/pull/35314
    // TODO: find a better solution than copy pasting this into every shared system that caches solution entities
    private void OnEntRemoved(Entity<BloodstreamComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and set it to null
        if (args.Entity == entity.Comp.BloodSolution?.Owner)
            entity.Comp.BloodSolution = null;

        if (args.Entity == entity.Comp.TemporarySolution?.Owner)
            entity.Comp.TemporarySolution = null;
    }

    private void OnReactionAttempt(Entity<BloodstreamComponent> ent, ref ReactionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var effect in args.Reaction.Effects)
        {
            switch (effect)
            {
                // TODO: Rather than this, ReactionAttempt should allow systems to remove effects from the list before the reaction.
                // TODO: I think there's a PR up on the repo for this and if there isn't I'll make one -Princess
                case EntityEffects.Effects.EntitySpawning.SpawnEntity: // Prevent entities from spawning in the bloodstream
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

    private void OnReactionAttempt(Entity<BloodstreamComponent> ent, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (args.Name != ent.Comp.BloodSolutionName
            && args.Name != ent.Comp.BloodTemporarySolutionName)
        {
            return;
        }

        OnReactionAttempt(ent, ref args.Event);
    }

    private void OnDamageChanged(Entity<BloodstreamComponent> ent, ref DamageChangedEvent args)
    {
        // The incoming state from the server raises a DamageChangedEvent as well.
        // But the changes to the bloodstream have also been dirtied,
        // so we prevent applying them twice.
        if (_timing.ApplyingState)
            return;

        if (args.DamageDelta is null || !args.DamageIncreased)
        {
            return;
        }

        // TODO probably cache this or something. humans get hurt a lot
        if (!PrototypeManager.Resolve(ent.Comp.DamageBleedModifiers, out var modifiers))
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
        TryModifyBleedAmount(ent.AsNullable(), totalFloat);

        /// Critical hit. Causes target to lose blood, using the bleed rate modifier of the weapon, currently divided by 5
        /// The crit chance is currently the bleed rate modifier divided by 25.
        /// Higher damage weapons have a higher chance to crit!

        // TODO: Replace with RandomPredicted once the engine PR is merged
        // Use both the receiver and the damage causing entity for the seed so that we have different results for multiple attacks in the same tick
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id, GetNetEntity(args.Origin)?.Id ?? 0 );
        var rand = new System.Random(seed);
        var prob = Math.Clamp(totalFloat / 25, 0, 1);
        if (totalFloat > 0 && rand.Prob(prob))
        {
            TryBleedOut(ent.AsNullable(), total / 5);
            _audio.PlayPredicted(ent.Comp.InstantBloodSound, ent, args.Origin);
        }

        // Heat damage will cauterize, causing the bleed rate to be reduced.
        else if (totalFloat <= ent.Comp.BloodHealedSoundThreshold && oldBleedAmount > 0)
        {
            // Magically, this damage has healed some bleeding, likely
            // because it's burn damage that cauterized their wounds.

            // We'll play a special sound and popup for feedback.
            _popup.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"), ent,
                    ent, PopupType.Medium); // only the burned entity can see this
            _audio.PlayPredicted(ent.Comp.BloodHealedSound, ent, args.Origin);
        }
    }

    /// <summary>
    /// Shows text on health examine, based on bleed rate and blood level.
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
        if (GetBloodLevel(ent.AsNullable()) < ent.Comp.BloodlossThreshold)
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("bloodstream-component-looks-pale", ("target", ent.Owner)));
        }
    }

    private void OnBeingGibbed(Entity<BloodstreamComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        SpillAllSolutions(ent.AsNullable());
    }

    private void OnApplyMetabolicMultiplier(Entity<BloodstreamComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.UpdateIntervalMultiplier));
    }

    private void OnRejuvenate(Entity<BloodstreamComponent> ent, ref RejuvenateEvent args)
    {
        TryModifyBleedAmount(ent.AsNullable(), -ent.Comp.BleedAmount);

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution))
        {
            SolutionContainer.RemoveAllSolution(ent.Comp.BloodSolution.Value);
            TryModifyBloodLevel(ent.AsNullable(), ent.Comp.BloodReferenceSolution.Volume);
        }
    }

    private void OnMetabolismExclusion(Entity<BloodstreamComponent> ent, ref MetabolismExclusionEvent args)
    {
        // Adding all blood reagents for filtering blood in metabolizer
        foreach (var (reagent, _) in ent.Comp.BloodReferenceSolution)
        {
            args.Reagents.Add(reagent);
        }
    }

    /// <summary>
    /// This returns the minimum amount of *usable* blood.
    /// For multi reagent bloodstreams, if you have 100 of Reagent Y need 100, and 50 of Reagent X and need 100,
    /// this will return 0.5f
    /// </summary>
    /// <returns>Returns the current blood level as a value from 0 to <see cref="BloodstreamComponent.MaxVolumeModifier"/></returns>
    public float GetBloodLevel(Entity<BloodstreamComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp)
            || !SolutionContainer.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution)
            || entity.Comp.BloodReferenceSolution.Volume == 0)
        {
            return 0.0f;
        }

        var totalBloodLevel = FixedPoint2.New(entity.Comp.MaxVolumeModifier); // Can't go above max volume factor...

        foreach (var (reagentId, quantity) in entity.Comp.BloodReferenceSolution.Contents)
        {
            // Ideally we use a different calculation for blood pressure, this just defines how much *usable* blood you have!
            totalBloodLevel = FixedPoint2.Min(totalBloodLevel, bloodSolution.GetTotalPrototypeQuantity(reagentId.Prototype) / quantity);
        }

        return (float)totalBloodLevel;
    }

    /// <summary>
    /// Setter for the BloodlossThreshold datafield.
    /// </summary>
    public void SetBloodLossThreshold(Entity<BloodstreamComponent?> ent, float threshold)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.BloodlossThreshold = threshold;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodlossThreshold));
    }

    /// <summary>
    /// Attempt to transfer a provided solution to internal solution.
    /// </summary>
    public bool TryAddToBloodstream(Entity<BloodstreamComponent?> ent, Solution solution)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution))
            return false;

        if (SolutionContainer.TryAddSolution(ent.Comp.BloodSolution.Value, solution))
            return true;

        return false;
    }

    /// <summary>
    /// Removes a certain amount of all reagents except of a single excluded one from the bloodstream and blood itself.
    /// </summary>
    /// <returns>
    /// Solution of removed chemicals or null if none were removed.
    /// </returns>
    public Solution? FlushChemicals(Entity<BloodstreamComponent?> ent, FixedPoint2 quantity, ProtoId<ReagentPrototype>? excludedReagent = null )
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return null;

        var flushedSolution = new Solution();

        for (var i = bloodSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = bloodSolution.Contents[i];
            if (ent.Comp.BloodReferenceSolution.ContainsPrototype(reagentId.Prototype) || reagentId.Prototype == excludedReagent)
                continue;

            var reagentFlushAmount = SolutionContainer.RemoveReagent(ent.Comp.BloodSolution.Value, reagentId, quantity);
            flushedSolution.AddReagent(reagentId, reagentFlushAmount);
        }

        return flushedSolution.Volume == 0 ? null : flushedSolution;
    }

    /// <summary>
    /// A simple helper that tries to move blood volume up or down by a specified amount.
    /// Blood will not go over normal volume for this entity's bloodstream.
    /// </summary>
    public bool TryModifyBloodLevel(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        var reference = 1f;

        if (amount < 0)
        {
            reference = 0f;
            amount *= -1;
        }

        return TryRegulateBloodLevel(ent, amount, reference);
    }

    /// <summary>
    /// Attempts to bring an entity's blood level to a modified equilibrium volume.
    /// </summary>
    /// <param name="ent">Entity whose bloodstream we're modifying.</param>
    /// <param name="amount">The absolute maximum amount of blood we can add or remove.</param>
    /// <param name="referenceFactor">The modifier for an entity's blood equilibrium, try to hit an entity's default blood volume multiplied by this value.</param>
    /// <remarks>This CANNOT go above maximum blood volume!</remarks>
    /// <returns>False if we were unable to regulate blood level. This may return true even if blood level doesn't change!</returns>
    public bool TryRegulateBloodLevel(Entity<BloodstreamComponent?> ent, FixedPoint2 amount, float referenceFactor = 1f)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution)
            || amount == 0)
            return false;

        referenceFactor = Math.Clamp(referenceFactor, 0f, ent.Comp.MaxVolumeModifier);

        foreach (var (referenceReagent, referenceQuantity) in ent.Comp.BloodReferenceSolution)
        {
            var error = referenceQuantity * referenceFactor - bloodSolution.GetTotalPrototypeQuantity(referenceReagent.Prototype);
            var adjustedAmount = amount * referenceQuantity / ent.Comp.BloodReferenceSolution.Volume;

            if (error > 0)
            {
                error = FixedPoint2.Min(error, adjustedAmount);
                bloodSolution.AddReagent(referenceReagent, error);
            }
            else if (error < 0)
            {
                // invert the error since we're removing reagents...
                error = FixedPoint2.Min( -error, adjustedAmount);
                bloodSolution.RemoveReagent(referenceReagent, error);
            }
        }

        return true;
    }

    public void TickBleed(Entity<BloodstreamComponent> entity)
    {
        // Removes blood from the bloodstream based on bleed amount (bleed rate)
        // as well as stop their bleeding to a certain extent.
        if (entity.Comp.BleedAmount <= 0)
            return;

        var ev = new BleedModifierEvent(entity.Comp.BleedAmount, entity.Comp.BleedReductionAmount);
        RaiseLocalEvent(entity, ref ev);

        // Blood is removed from the bloodstream at a 1-1 rate with the bleed amount
        TryBleedOut(entity.AsNullable(), ev.BleedAmount);

        // Bleed rate is reduced by the bleed reduction amount in the bloodstream component.
        TryModifyBleedAmount(entity.AsNullable(), -ev.BleedReductionAmount);
    }

    /// <summary>
    /// Removes blood by spilling out the bloodstream.
    /// </summary>
    public bool TryBleedOut(Entity<BloodstreamComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || !SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution)
            || amount <= 0)
        {
            return false;
        }

        var leakedBlood = SolutionContainer.SplitSolution(ent.Comp.BloodSolution.Value, amount);

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodTemporarySolutionName, ref ent.Comp.TemporarySolution, out var tempSolution))
            return true;

        tempSolution.AddSolution(leakedBlood, PrototypeManager);

        if (tempSolution.Volume > ent.Comp.BleedPuddleThreshold)
        {
            _puddle.TrySpillAt(ent.Owner, tempSolution, out _, sound: false);

            tempSolution.RemoveAllSolution();
        }

        SolutionContainer.UpdateChemicals(ent.Comp.TemporarySolution.Value);

        return true;
    }

    /// <summary>
    /// Tries to make an entity bleed more or less.
    /// </summary>
    public bool TryModifyBleedAmount(Entity<BloodstreamComponent?> ent, float amount)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        ent.Comp.BleedAmount += amount;
        ent.Comp.BleedAmount = Math.Clamp(ent.Comp.BleedAmount, 0, ent.Comp.MaxBleedAmount);

        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BleedAmount));

        if (ent.Comp.BleedAmount == 0)
            _alertsSystem.ClearAlert(ent.Owner, ent.Comp.BleedingAlert);
        else
        {
            var severity = (short)Math.Clamp(Math.Round(ent.Comp.BleedAmount, MidpointRounding.ToZero), 0, 10);
            _alertsSystem.ShowAlert(ent.Owner, ent.Comp.BleedingAlert, severity);
        }

        return true;
    }

    /// <summary>
    /// Spill all bloodstream solutions into a puddle.
    /// BLOOD FOR THE BLOOD GOD
    /// </summary>
    public void SpillAllSolutions(Entity<BloodstreamComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var tempSol = new Solution();

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
        {
            tempSol.MaxVolume += bloodSolution.MaxVolume;
            tempSol.AddSolution(bloodSolution, PrototypeManager);
            SolutionContainer.RemoveAllSolution(ent.Comp.BloodSolution.Value);
        }

        if (SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodTemporarySolutionName, ref ent.Comp.TemporarySolution, out var tempSolution))
        {
            tempSol.MaxVolume += tempSolution.MaxVolume;
            tempSol.AddSolution(tempSolution, PrototypeManager);
            SolutionContainer.RemoveAllSolution(ent.Comp.TemporarySolution.Value);
        }

        _puddle.TrySpillAt(ent, tempSol, out _);
    }

    /// <summary>
    /// Change what someone's blood is made of, on the fly.
    /// </summary>
    [Obsolete("ChangeBloodReagent is obsolete, please use ChangeBloodReagents.")]
    public void ChangeBloodReagent(Entity<BloodstreamComponent?> ent, ProtoId<ReagentPrototype> reagent)
    {
        ChangeBloodReagents(ent, new([new(reagent, 1)]));
    }

    /// <summary>
    /// Change what someone's blood is made of, on the fly.
    /// </summary>
    public void ChangeBloodReagents(Entity<BloodstreamComponent?> ent, Solution reagents)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
        {
            return;
        }

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
        {
            ent.Comp.BloodReferenceSolution = reagents.Clone();
            DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodReferenceSolution));
            return;
        }

        var currentVolume = FixedPoint2.Zero;
        foreach (var reagent in ent.Comp.BloodReferenceSolution)
        {
            currentVolume += bloodSolution.RemoveReagent(reagent.Reagent, quantity: bloodSolution.Volume, ignoreReagentData: true);
        }

        ent.Comp.BloodReferenceSolution = reagents.Clone();
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodReferenceSolution));

        if (currentVolume == FixedPoint2.Zero)
            return;

        var solution = ent.Comp.BloodReferenceSolution.Clone();
        solution.ScaleSolution(currentVolume / solution.Volume);
        SolutionContainer.AddSolution(ent.Comp.BloodSolution.Value, solution);
    }

    /// <summary>
    /// Get the reagent data for blood that a specific entity should have.
    /// </summary>
    public List<ReagentData> GetEntityBloodData(Entity<BloodstreamComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return NewEntityBloodData(entity);

        return entity.Comp.BloodData ?? NewEntityBloodData(entity);
    }

    /// <summary>
    /// Gets new blood data for this entity and caches it in <see cref="BloodstreamComponent.BloodData"/>
    /// </summary>
    protected List<ReagentData> NewEntityBloodData(EntityUid uid)
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
