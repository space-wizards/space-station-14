using Content.Server.Administration.Logs;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
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
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

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
                        Inhale(uid, body);
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
                if (_gameTiming.CurTime >= respirator.LastGaspPopupTime + respirator.GaspPopupCooldown)
                {
                    respirator.LastGaspPopupTime = _gameTiming.CurTime;
                    _popupSystem.PopupEntity(Loc.GetString("lung-behavior-gasp"), uid);
                }

                TakeSuffocationDamage((uid, respirator));
                respirator.SuffocationCycles += 1;
                continue;
            }

            StopSuffocation((uid, respirator));
            respirator.SuffocationCycles = 0;
        }
    }

    public void Inhale(EntityUid uid, BodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, logMissing: false))
            return;

        var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(uid, body);

        // Inhale gas
        var ev = new InhaleLocationEvent();
        RaiseLocalEvent(uid, ref ev, broadcast: false);

        ev.Gas ??= _atmosSys.GetContainingMixture(uid, excite: true);

        if (ev.Gas is null)
        {
            return;
        }

        var actualGas = ev.Gas.RemoveVolume(Atmospherics.BreathVolume);

        var lungRatio = 1.0f / organs.Count;
        var gas = organs.Count == 1 ? actualGas : actualGas.RemoveRatio(lungRatio);
        foreach (var (lung, _) in organs)
        {
            // Merge doesn't remove gas from the giver.
            _atmosSys.Merge(lung.Air, gas);
            _lungSystem.GasToReagent(lung.Owner, lung);
        }
    }

    public void Exhale(EntityUid uid, BodyComponent? body = null)
    {
        if (!Resolve(uid, ref body, logMissing: false))
            return;

        var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(uid, body);

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
        foreach (var (lung, _) in organs)
        {
            _atmosSys.Merge(outGas, lung.Air);
            lung.Air.Clear();

            if (_solutionContainerSystem.ResolveSolution(lung.Owner, lung.SolutionName, ref lung.Solution))
                _solutionContainerSystem.RemoveAllSolution(lung.Solution.Value);
        }

        _atmosSys.Merge(ev.Gas, outGas);
    }

    private void TakeSuffocationDamage(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles == 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} started suffocating");

        if (ent.Comp.SuffocationCycles >= ent.Comp.SuffocationCycleThreshold)
        {
            // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
            var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(ent);
            foreach (var (comp, _) in organs)
            {
                _alertsSystem.ShowAlert(ent, comp.Alert);
            }
        }

        _damageableSys.TryChangeDamage(ent, ent.Comp.Damage, interruptsDoAfters: false);
    }

    private void StopSuffocation(Entity<RespiratorComponent> ent)
    {
        if (ent.Comp.SuffocationCycles >= 2)
            _adminLogger.Add(LogType.Asphyxiation, $"{ToPrettyString(ent):entity} stopped suffocating");

        // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
        var organs = _bodySystem.GetBodyOrganComponents<LungComponent>(ent);
        foreach (var (comp, _) in organs)
        {
            _alertsSystem.ClearAlert(ent, comp.Alert);
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
