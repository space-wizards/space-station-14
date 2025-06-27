using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.EntityEffects;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.EffectConditions;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems;

[UsedImplicitly]
public sealed class RespiratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSys = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSys = default!;
    [Dependency] private readonly LungSystem _lungSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityEffectSystem _entityEffect = default!;

    private static readonly ProtoId<MetabolismGroupPrototype> GasId = new("Gas");

    // High/Low temp alerts
    private static readonly ProtoId<AlertPrototype> HighTempAlertId = new("AirHot");
    private static readonly ProtoId<AlertPrototype> LowTempAlertId = new("AirCold");
    private static readonly ProtoId<AlertCategoryPrototype> TempAlertCategory = new("AirTemperature");

    public override void Initialize()
    {
        base.Initialize();

        // We want to process lung reagents before we inhale new reagents.
        UpdatesAfter.Add(typeof(MetabolizerSystem));
        SubscribeLocalEvent<RespiratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RespiratorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<RespiratorComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnMapInit(Entity<RespiratorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnUnpaused(Entity<RespiratorComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextUpdate += args.PausedTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RespiratorComponent, BodyComponent>();
        while (query.MoveNext(out var uid, out var respirator, out var body))
        {
            if (_gameTiming.CurTime < respirator.NextUpdate)
                continue;

            respirator.NextUpdate += respirator.UpdateInterval;

            if (_mobState.IsDead(uid))
                continue;

            UpdateSaturation(uid, -(float) respirator.UpdateInterval.TotalSeconds, respirator);

            if (!_mobState.IsIncapacitated(uid)) // cannot breathe in crit.
            {
                switch (respirator.Status)
                {
                    case RespiratorStatus.Inhaling:
                        Inhale((uid, respirator, body));
                        respirator.Status = RespiratorStatus.Exhaling;
                        break;
                    case RespiratorStatus.Exhaling:
                        Exhale(uid, body);
                        respirator.Status = RespiratorStatus.Inhaling;
                        break;
                }
            }

            if (respirator.Saturation < respirator.SuffocationThreshold)
            {
                if (_gameTiming.CurTime >= respirator.LastGaspEmoteTime + respirator.GaspEmoteCooldown)
                {
                    respirator.LastGaspEmoteTime = _gameTiming.CurTime;
                    _chat.TryEmoteWithChat(uid, respirator.GaspEmote, ChatTransmitRange.HideChat, ignoreActionBlocker: true);
                }

                TakeSuffocationDamage((uid, respirator));
                respirator.SuffocationCycles += 1;
                continue;
            }

            StopSuffocation((uid, respirator));
            respirator.SuffocationCycles = 0;
        }
    }

    public void Inhale(Entity<RespiratorComponent?, BodyComponent?, ThermalRegulatorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, logMissing: false))
            return;

        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, ent.Comp2));

        // Inhale gas
        var ev = new InhaleLocationEvent();
        RaiseLocalEvent(ent, ref ev);

        ev.Gas ??= _atmosSys.GetContainingMixture(ent.Owner, excite: true);

        if (ev.Gas is null)
        {
            return;
        }

        // Remove a volume of gas from the environment
        var actualGas = ev.Gas.RemoveVolume(Atmospherics.BreathVolume);

        // Only apply temperature damage if there are actually moles to breathe
        // This prevents damage from vacuum which has temperature but no actual gas
        if (actualGas.TotalMoles > Atmospherics.GasMinMoles)
        {
            var gasTemp = actualGas.Temperature;

            // Take the normal body temperature from the mob's ThermalRegulatorComponent
            // We don't resolve this component because it shouldn't be required, so we fall back to the atmos consts
            var normalBodyTempK = ent.Comp3?.NormalBodyTemperature ?? Atmospherics.T0C + Atmospherics.NormalBodyTemperature;
            var heatCapacity = _atmosSys.GetHeatCapacity(actualGas, false);

            // Calculate thermal energy difference between inhaled gas and normal body temperature
            // This represents how much thermal energy would be transferred to/from the body
            var normalThermalEnergy = heatCapacity * normalBodyTempK;
            var gasThermalEnergy = _atmosSys.GetThermalEnergy(actualGas);
            var energyDifference = Math.Abs(gasThermalEnergy - normalThermalEnergy);

            // Constants for damage scaling
            // Oxygen at -200°C (73.15K) with its heat capacity will cause maximum damage (5x multiplier)
            const float baseDamageEnergyThreshold = 25f;

            // Cap damage multiplier at 5x to prevent extremely hot/cold small amounts of gas from dealing excessive damage
            const float maxDamageMultiplier = 5.0f;

            // Calculate a damage scale factor based on thermal energy difference
            // The factor starts at 1.0 at the base threshold and increases linearly with energy
            var energyScaleFactor = 1.0f + (energyDifference / baseDamageEnergyThreshold);
            energyScaleFactor = Math.Min(energyScaleFactor, maxDamageMultiplier);

            // Apply high temperature damage
            if (gasTemp > ent.Comp1.HighTemperatureDamageThreshold)
            {
                var scaledDamage = ent.Comp1.HighTemperatureDamage * energyScaleFactor;
                _damageableSys.TryChangeDamage(ent, scaledDamage, true);

                // Show the alert for the player
                _alertsSystem.ShowAlert(ent, HighTempAlertId);

                // Log damage for admins with energy information
                _adminLogger.Add(LogType.Temperature,
                    $"{ToPrettyString(ent):entity} took {scaledDamage.GetTotal():damage} breathing damage");
            }
            // Apply low temperature damage
            else if (gasTemp < ent.Comp1.LowTemperatureDamageThreshold)
            {
                var scaledDamage = ent.Comp1.LowTemperatureDamage * energyScaleFactor;
                _damageableSys.TryChangeDamage(ent, scaledDamage, true);

                // Show the alert for the player
                _alertsSystem.ShowAlert(ent, LowTempAlertId);

                // Log damage for admins with energy information
                _adminLogger.Add(LogType.Temperature,
                    $"{ToPrettyString(ent):entity} took {scaledDamage.GetTotal():damage} breathing damage ");
            }
            else
            {
                // Not taking damage, clear the alerts
                _alertsSystem.ClearAlertCategory(ent, TempAlertCategory);
            }
        }
        else
        {
            // Not enough gas to cause temperature damage, clear the alerts
            _alertsSystem.ClearAlertCategory(ent, TempAlertCategory);
        }

        var lungRatio = 1.0f / organs.Count;
        var gas = organs.Count == 1 ? actualGas : actualGas.RemoveRatio(lungRatio);
        foreach (var (organUid, lung, _) in organs)
        {
            // Merge doesn't remove gas from the giver.
            _atmosSys.Merge(lung.Air, gas);
            _lungSystem.GasToReagent(organUid, lung);
        }
    }

    public void Exhale(EntityUid uid, BodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, logMissing: false))
            return;

        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((uid, body));

        // exhale gas

        var ev = new ExhaleLocationEvent();
        RaiseLocalEvent(uid, ref ev, broadcast: false);

        if (ev.Gas is null)
        {
            ev.Gas = _atmosSys.GetContainingMixture(uid, excite: true);

            // Walls and grids without atmos comp return null. I guess it makes sense to not be able to exhale in walls,
            // but this also means you cannot exhale on some grids.
            ev.Gas ??= GasMixture.SpaceGas;
        }

        var outGas = new GasMixture(ev.Gas.Volume);
        foreach (var (organUid, lung, _) in organs)
        {
            _atmosSys.Merge(outGas, lung.Air);
            lung.Air.Clear();

            if (_solutionContainerSystem.ResolveSolution(organUid, lung.SolutionName, ref lung.Solution))
                _solutionContainerSystem.RemoveAllSolution(lung.Solution.Value);
        }

        _atmosSys.Merge(ev.Gas, outGas);
    }

    /// <summary>
    /// Returns true if the entity is above their SuffocationThreshold and alive.
    /// </summary>
    public bool IsBreathing(Entity<RespiratorComponent?> ent)
    {
        if (_mobState.IsIncapacitated(ent))
            return false;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        return (ent.Comp.Saturation > ent.Comp.SuffocationThreshold);
    }

    /// <summary>
    /// Check whether or not an entity can metabolize inhaled air without suffocating or taking damage (i.e., no toxic
    /// gasses).
    /// </summary>
    public bool CanMetabolizeInhaledAir(Entity<RespiratorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var ev = new InhaleLocationEvent();
        RaiseLocalEvent(ent, ref ev);

        var gas = ev.Gas ?? _atmosSys.GetContainingMixture(ent.Owner);
        if (gas == null)
            return false;

        return CanMetabolizeGas(ent, gas);
    }

    /// <summary>
    /// Check whether or not an entity can metabolize the given gas mixture without suffocating or taking damage
    /// (i.e., no toxic gasses).
    /// </summary>
    public bool CanMetabolizeGas(Entity<RespiratorComponent?> ent, GasMixture gas)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, null));
        if (organs.Count == 0)
            return false;

        gas = new GasMixture(gas);
        var lungRatio = 1.0f / organs.Count;
        gas.Multiply(MathF.Min(lungRatio * gas.Volume/Atmospherics.BreathVolume, lungRatio));
        var solution = _lungSystem.GasToReagent(gas);

        float saturation = 0;
        foreach (var organ in organs)
        {
            saturation += GetSaturation(solution, organ.Owner, out var toxic);
            if (toxic)
                return false;
        }

        return saturation > ent.Comp.UpdateInterval.TotalSeconds;
    }

    /// <summary>
    /// Get the amount of saturation that would be generated if the lung were to metabolize the given solution.
    /// </summary>
    /// <remarks>
    /// This assumes the metabolism rate is unbounded, which generally should be the case for lungs, otherwise we get
    /// back to the old pulmonary edema bug.
    /// </remarks>
    /// <param name="solution">The reagents to metabolize</param>
    /// <param name="lung">The entity doing the metabolizing</param>
    /// <param name="toxic">Whether or not any of the reagents would deal damage to the entity</param>
    private float GetSaturation(Solution solution, Entity<MetabolizerComponent?> lung, out bool toxic)
    {
        toxic = false;
        if (!Resolve(lung, ref lung.Comp))
            return 0;

        if (lung.Comp.MetabolismGroups == null)
            return 0;

        float saturation = 0;
        foreach (var (id, quantity) in solution.Contents)
        {
            var reagent = _protoMan.Index<ReagentPrototype>(id.Prototype);
            if (reagent.Metabolisms == null)
                continue;

            if (!reagent.Metabolisms.TryGetValue(GasId, out var entry))
                continue;

            foreach (var effect in entry.Effects)
            {
                if (effect is HealthChange health)
                    toxic |= CanMetabolize(health) && health.Damage.AnyPositive();
                else if (effect is Oxygenate oxy && CanMetabolize(oxy))
                    saturation += oxy.Factor * quantity.Float();
            }
        }

        // TODO generalize condition checks
        // this is pretty janky, but I just want to bodge a method that checks if an entity can breathe a gas mixture
        // Applying actual reaction effects require a full ReagentEffectArgs struct.
        bool CanMetabolize(EntityEffect effect)
        {
            if (effect.Conditions == null)
                return true;

            foreach (var cond in effect.Conditions)
            {
                if (cond is OrganType organ && !_entityEffect.OrganCondition(organ, lung))
                    return false;
            }

            return true;
        }

        return saturation;
    }

    private void TakeSuffocationDamage(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles == 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} started suffocating");

        if (ent.Comp.SuffocationCycles >= ent.Comp.SuffocationCycleThreshold)
        {
            // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
            var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, null));
            foreach (var entity in organs)
            {
                _alertsSystem.ShowAlert(ent, entity.Comp1.Alert);
            }
        }

        _damageableSys.TryChangeDamage(ent, ent.Comp.Damage, interruptsDoAfters: false);
    }

    private void StopSuffocation(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles >= 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} stopped suffocating");

        // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, null));
        foreach (var entity in organs)
        {
            _alertsSystem.ClearAlert(ent, entity.Comp1.Alert);
        }

        _damageableSys.TryChangeDamage(ent, ent.Comp.DamageRecovery);
    }

    public void UpdateSaturation(EntityUid uid, float amount,
        RespiratorComponent? respirator = null)
    {
        if (!Resolve(uid, ref respirator, false))
            return;

        respirator.Saturation += amount;
        respirator.Saturation =
            Math.Clamp(respirator.Saturation, respirator.MinSaturation, respirator.MaxSaturation);
    }

    private void OnApplyMetabolicMultiplier(
        Entity<RespiratorComponent> ent,
        ref ApplyMetabolicMultiplierEvent args)
    {
        // TODO REFACTOR THIS
        // This will slowly drift over time due to floating point errors.
        // Instead, raise an event with the base rates and allow modifiers to get applied to it.
        if (args.Apply)
        {
            ent.Comp.UpdateInterval *= args.Multiplier;
            ent.Comp.Saturation *= args.Multiplier;
            ent.Comp.MaxSaturation *= args.Multiplier;
            ent.Comp.MinSaturation *= args.Multiplier;
            return;
        }

        // This way we don't have to worry about it breaking if the stasis bed component is destroyed
        ent.Comp.UpdateInterval /= args.Multiplier;
        ent.Comp.Saturation /= args.Multiplier;
        ent.Comp.MaxSaturation /= args.Multiplier;
        ent.Comp.MinSaturation /= args.Multiplier;
    }
}

[ByRefEvent]
public record struct InhaleLocationEvent(GasMixture? Gas);

[ByRefEvent]
public record struct ExhaleLocationEvent(GasMixture? Gas);
