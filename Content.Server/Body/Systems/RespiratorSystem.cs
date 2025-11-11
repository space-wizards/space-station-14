using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.EntityConditions;
using Content.Shared.EntityConditions.Conditions.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.EntityEffects.Effects.Body;
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
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosSys = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damageableSys = default!;
    [Dependency] private readonly LungSystem _lungSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _entityConditions = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    private static readonly ProtoId<MetabolismGroupPrototype> GasId = new("Gas");

    public override void Initialize()
    {
        base.Initialize();

        // We want to process lung reagents before we inhale new reagents.
        UpdatesAfter.Add(typeof(MetabolizerSystem));
        SubscribeLocalEvent<RespiratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RespiratorComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);

        // BodyComp stuff
        SubscribeLocalEvent<BodyComponent, InhaledGasEvent>(OnGasInhaled);
        SubscribeLocalEvent<BodyComponent, ExhaledGasEvent>(OnGasExhaled);
        SubscribeLocalEvent<BodyComponent, CanMetabolizeGasEvent>(CanBodyMetabolizeGas);
        SubscribeLocalEvent<BodyComponent, SuffocationEvent>(OnSuffocation);
        SubscribeLocalEvent<BodyComponent, StopSuffocatingEvent>(OnStopSuffocating);
    }

    private void OnMapInit(Entity<RespiratorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.AdjustedUpdateInterval;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RespiratorComponent>();
        while (query.MoveNext(out var uid, out var respirator))
        {
            if (_gameTiming.CurTime < respirator.NextUpdate)
                continue;

            respirator.NextUpdate += respirator.AdjustedUpdateInterval;

            if (_mobState.IsDead(uid))
                continue;

            UpdateSaturation(uid, -(float)respirator.UpdateInterval.TotalSeconds, respirator);

            if (!_mobState.IsIncapacitated(uid)) // cannot breathe in crit.
            {
                switch (respirator.Status)
                {
                    case RespiratorStatus.Inhaling:
                        Inhale((uid, respirator));
                        respirator.Status = RespiratorStatus.Exhaling;
                        break;
                    case RespiratorStatus.Exhaling:
                        Exhale((uid, respirator));
                        respirator.Status = RespiratorStatus.Inhaling;
                        break;
                }
            }

            if (respirator.Saturation < respirator.SuffocationThreshold)
            {
                if (_gameTiming.CurTime >= respirator.LastGaspEmoteTime + respirator.GaspEmoteCooldown)
                {
                    respirator.LastGaspEmoteTime = _gameTiming.CurTime;
                    _chat.TryEmoteWithChat(uid,
                        respirator.GaspEmote,
                        ChatTransmitRange.HideChat,
                        ignoreActionBlocker: true);
                }

                TakeSuffocationDamage((uid, respirator));
                respirator.SuffocationCycles += 1;
                continue;
            }

            StopSuffocation((uid, respirator));
            respirator.SuffocationCycles = 0;
        }
    }

    public void Inhale(Entity<RespiratorComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        // Inhale gas
        var ev = new InhaleLocationEvent
        {
            Respirator = entity.Comp,
        };
        RaiseLocalEvent(entity, ref ev);

        ev.Gas ??= _atmosSys.GetContainingMixture(entity.Owner, excite: true);

        if (ev.Gas is null)
            return;

        var gas = ev.Gas.RemoveVolume(entity.Comp.BreathVolume);

        var inhaleEv = new InhaledGasEvent(gas);
        RaiseLocalEvent(entity, ref inhaleEv);

        if (inhaleEv.Handled && inhaleEv.Succeeded)
            return;

        // If nothing could inhale the gas give it back.
        _atmosSys.Merge(ev.Gas, gas);
    }

    public void Exhale(Entity<RespiratorComponent> entity)
    {
        // exhale gas

        var ev = new ExhaleLocationEvent();
        RaiseLocalEvent(entity, ref ev, broadcast: false);

        if (ev.Gas is null)
        {
            ev.Gas = _atmosSys.GetContainingMixture(entity.Owner, excite: true);

            // Walls and grids without atmos comp return null. I guess it makes sense to not be able to exhale in walls,
            // but this also means you cannot exhale on some grids.
            ev.Gas ??= GasMixture.SpaceGas;
        }

        Exhale(entity!, ev.Gas);
    }

    public void Exhale(Entity<RespiratorComponent?> entity, GasMixture gas)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        var ev = new ExhaledGasEvent(gas);
        RaiseLocalEvent(entity, ref ev);
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
    /// Checks if it's safe for a given entity to breathe the air from the environment it is currently situated in.
    /// </summary>
    /// <param name="ent">The entity attempting to metabolize the gas.</param>
    /// <returns>Returns true only if the air is not toxic, and it wouldn't suffocate.</returns>
    public bool CanMetabolizeInhaledAir(Entity<RespiratorComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Get the gas at our location but don't actually remove it from the gas mixture.
        var ev = new InhaleLocationEvent
        {
            Respirator = ent.Comp,
        };
        RaiseLocalEvent(ent, ref ev);

        ev.Gas ??= _atmosSys.GetContainingMixture(ent.Owner, excite: true);

        // If there's no air to breathe or we can't metabolize it then internals should be on.
        return ev.Gas is not null && CanMetabolizeInhaledAir(ent, ev.Gas);
    }

    /// <summary>
    /// Checks if a given entity can safely metabolize a given gas mixture.
    /// </summary>
    /// <param name="ent">The entity attempting to metabolize the gas.</param>
    /// <param name="gas">The gas mixture we are trying to metabolize.</param>
    /// <returns>Returns true only if the gas mixture is not toxic, and it wouldn't suffocate.</returns>
    public bool CanMetabolizeInhaledAir(Entity<RespiratorComponent?> ent, GasMixture gas)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var ev = new CanMetabolizeGasEvent(gas);
        RaiseLocalEvent(ent, ref ev);

        if (!ev.Handled || ev.Toxic)
            return false;

        return ev.Saturation > ent.Comp.UpdateInterval.TotalSeconds;
    }

    /// <summary>
    /// Tries to safely metabolize the current solutions in a body's lungs.
    /// </summary>
    private void CanBodyMetabolizeGas(Entity<BodyComponent> ent, ref CanMetabolizeGasEvent args)
    {
        if (args.Handled)
            return;

        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, null));
        if (organs.Count == 0)
            return;

        var solution = _lungSystem.GasToReagent(args.Gas);

        var saturation = 0f;
        foreach (var organ in organs)
        {
            saturation += GetSaturation(solution, organ.Owner, out var toxic);
            if (!toxic)
                continue;

            args.Handled = true;
            args.Toxic = true;
            return;
        }

        args.Handled = true;
        args.Saturation = saturation;
    }

    public bool TryInhaleGasToBody(Entity<BodyComponent?> entity, GasMixture gas)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((entity, entity.Comp));
        if (organs.Count == 0)
            return false;

        var lungRatio = 1.0f / organs.Count;
        var splitGas = organs.Count == 1 ? gas : gas.RemoveRatio(lungRatio);
        foreach (var (organUid, lung, _) in organs)
        {
            // Merge doesn't remove gas from the giver.
            _atmosSys.Merge(lung.Air, splitGas);
            _lungSystem.GasToReagent(organUid, lung);
        }

        return true;
    }

    public void RemoveGasFromBody(Entity<BodyComponent> ent, GasMixture gas)
    {
        var outGas = new GasMixture(gas.Volume);

        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, ent.Comp));
        if (organs.Count == 0)
            return;

        foreach (var (organUid, lung, _) in organs)
        {
            _atmosSys.Merge(outGas, lung.Air);
            lung.Air.Clear();

            if (_solutionContainerSystem.ResolveSolution(organUid, lung.SolutionName, ref lung.Solution))
                _solutionContainerSystem.RemoveAllSolution(lung.Solution.Value);
        }

        _atmosSys.Merge(gas, outGas);
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

        // this is pretty janky, but I just want to bodge a method that checks if an entity can breathe a gas mixture
        // Applying actual reaction effects require a full ReagentEffectArgs struct.
        bool CanMetabolize(EntityEffect effect)
        {
            if (effect.Conditions == null)
                return true;

            // TODO: Use Metabolism Public API to do this instead, once that API has been built.
            foreach (var cond in effect.Conditions)
            {
                if (cond is MetabolizerTypeCondition organ && !_entityConditions.TryCondition(lung, organ))
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

        _damageableSys.ChangeDamage(ent.Owner, ent.Comp.Damage, interruptsDoAfters: false);

        if (ent.Comp.SuffocationCycles < ent.Comp.SuffocationCycleThreshold)
            return;

        var ev = new SuffocationEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void StopSuffocation(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles >= 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} stopped suffocating");

        _damageableSys.ChangeDamage(ent.Owner, ent.Comp.DamageRecovery);

        var ev = new StopSuffocatingEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnSuffocation(Entity<BodyComponent> ent, ref SuffocationEvent args)
    {
        // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, null));
        foreach (var entity in organs)
        {
            _alertsSystem.ShowAlert(ent.Owner, entity.Comp1.Alert);
        }
    }

    private void OnStopSuffocating(Entity<BodyComponent> ent, ref StopSuffocatingEvent args)
    {
        // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
        var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((ent, null));
        foreach (var entity in organs)
        {
            _alertsSystem.ClearAlert(ent.Owner, entity.Comp1.Alert);
        }
    }

    public void UpdateSaturation(EntityUid uid, float amount, RespiratorComponent? respirator = null)
    {
        if (!Resolve(uid, ref respirator, false))
            return;

        respirator.Saturation += amount;
        respirator.Saturation =
            Math.Clamp(respirator.Saturation, respirator.MinSaturation, respirator.MaxSaturation);
    }

    private void OnApplyMetabolicMultiplier(Entity<RespiratorComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
    }

    private void OnGasInhaled(Entity<BodyComponent> entity, ref InhaledGasEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        args.Succeeded = TryInhaleGasToBody((entity, entity.Comp), args.Gas);
    }

    private void OnGasExhaled(Entity<BodyComponent> entity, ref ExhaledGasEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        RemoveGasFromBody(entity, args.Gas);
    }
}

/// <summary>
/// Event raised when an entity first tries to inhale that returns a GasMixture from a given location.
/// </summary>
/// <param name="Gas">The gas that gets returned, null if there is none.</param>
/// <param name="Respirator">The Respirator component of the entity attempting to inhale</param>
[ByRefEvent]
public record struct InhaleLocationEvent(GasMixture? Gas, RespiratorComponent Respirator);

/// <summary>
/// Event raised when an entity first tries to exhale a gas, determines where the gas they're exhaling will be sent.
/// </summary>
/// <param name="Gas">The gas mixture that the exhaled gas will be merged into.</param>
[ByRefEvent]
public record struct ExhaleLocationEvent(GasMixture? Gas);

/// <summary>
/// Event raised when an entity successfully inhales a gas, attempts to find a place to put the gas.
/// </summary>
/// <param name="Gas">The gas we're inhaling.</param>
/// <param name="Handled">Whether a system has responded appropriately.</param>
/// <param name="Succeeded">Whether we successfully managed to inhale the gas</param>
[ByRefEvent]
public record struct InhaledGasEvent(GasMixture Gas, bool Handled = false, bool Succeeded = false);

/// <summary>
/// Event raised when an entity is exhaling
/// </summary>
/// <param name="Gas">The gas mixture we're exhaling into.</param>
/// <param name="Handled">Whether we have successfully exhaled or not.</param>
[ByRefEvent]
public record struct ExhaledGasEvent(GasMixture Gas, bool Handled = false);

/// <summary>
/// Raised when an entity starts suffocating and when suffocation progresses.
/// </summary>
[ByRefEvent]
public record struct SuffocationEvent;

/// <summary>
/// Raised when an entity that was suffocating stops suffocating.
/// </summary>
[ByRefEvent]
public record struct StopSuffocatingEvent;

/// <summary>
/// An event raised to inhalation handlers that asks them nicely to simulate what it would be like to metabolize
/// a given volume of gas, without actually metabolizing it.
/// </summary>
/// <param name="Gas">The gas mixture we are testing.</param>
/// <param name="Toxic">Whether the gas returns as toxic to any respirator.</param>
/// <param name="Saturation">The amount of saturation we got from the gas.</param>
[ByRefEvent]
public record struct CanMetabolizeGasEvent(GasMixture Gas, bool Toxic = false, float Saturation = 0f, bool Handled = false);
