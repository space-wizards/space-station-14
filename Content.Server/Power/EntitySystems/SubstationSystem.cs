using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.CCVar;
using Content.Shared.Power;
using Content.Shared.Rejuvenate;
using Content.Shared.Wires;
using Content.Shared.Tag;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Content.Server.Atmos.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

public sealed class SubstationSystem : EntitySystem
{
    [Dependency] private readonly PointLightSystem _lightSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _sharedLightSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _substationDecayEnabled;
    private int _substationDecayTimeout;
    private float _substationDecayCoefficient;
    private float _substationDecayTimer;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        _substationDecayEnabled = _cfg.GetCVar(CCVars.SubstationDecayEnabled);
        _substationDecayTimeout = _cfg.GetCVar(CCVars.SubstationDecayTimeout);
        _substationDecayCoefficient = _cfg.GetCVar(CCVars.SubstationDecayCoefficient);
        _substationDecayTimer = _cfg.GetCVar(CCVars.SubstationDecayTimer);

        SubscribeLocalEvent<SubstationComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SubstationComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<SubstationComponent, GasAnalyzerScanEvent>(OnAnalyzed);

        SubscribeLocalEvent<SubstationComponent, EntInsertedIntoContainerMessage>(OnNitrogenBoosterInserted);
        SubscribeLocalEvent<SubstationComponent, EntRemovedFromContainerMessage>(OnNitrogenBoosterRemoved);
        SubscribeLocalEvent<SubstationComponent, ContainerIsInsertingAttemptEvent>(OnNitrogenBoosterInsertAttempt);
        SubscribeLocalEvent<SubstationComponent, ContainerIsRemovingAttemptEvent>(OnNitrogenBoosterRemoveAttempt);
    }

    private void OnExamine(EntityUid uid, SubstationComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (!GetNitrogenBoosterMixture(uid, out var mix))
            {
                args.PushMarkup(Loc.GetString("substation-component-examine-no-nitrogenbooster"));
                return;
            }

            var integrity = CheckNitrogenBoosterIntegrity(component, mix);
            if (integrity > 0.0f)
            {
                var integrityPercentRounded = (int)integrity;
                args.PushMarkup(Loc.GetString("substation-component-examine-integrity", ("percent", integrityPercentRounded), ("markupPercentColor", "green")));
            }
            else
            {
                args.PushMarkup(Loc.GetString("substation-component-examine-malfunction"));
            }
        }
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var lightQuery = EntityQueryEnumerator<SubstationComponent>();
        while (lightQuery.MoveNext(out var uid, out var subs))
        {
            subs.SubstationLightBlinkTimer -= deltaTime;
            if (subs.SubstationLightBlinkTimer <= 0f)
            {
                subs.SubstationLightBlinkTimer = subs.SubstationLightBlinkInterval;
                subs.SubstationLightBlinkState = !subs.SubstationLightBlinkState;

                if (subs.State == SubstationIntegrityState.Healthy)
                    continue;

                if (!_lightSystem.TryGetLight(uid, out var shlight))
                    return;

                _sharedLightSystem.SetEnergy(uid, subs.SubstationLightBlinkState ? 1.6f : 1f, shlight);
            }

            if (!_substationDecayEnabled)
            {
                _substationDecayTimer -= deltaTime;
                if (_substationDecayTimer <= 0.0f)
                {
                    _substationDecayTimer = 0.0f;
                    _substationDecayEnabled = true;
                }
                return;
            }
        }
    }

    private void ConsumeNitrogenBoosterGas(float deltaTime, float scalar, SubstationComponent subs, PowerNetworkBatteryComponent battery, GasMixture mixture)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var boosterMoles = subs.InitialNitrogenBoosterMoles;
        var currentSupply = battery.CurrentSupply;
        var decayFactor = _substationDecayCoefficient * scalar;

        var molesConsumed = boosterMoles * currentSupply * deltaTime / decayFactor;
        var minimumReaction = Math.Abs(initialN2) * molesConsumed / 2;

        mixture.AdjustMoles(Gas.Nitrogen, -minimumReaction);
        mixture.AdjustMoles(Gas.NitrousOxide, minimumReaction);
    }

    private float CheckNitrogenBoosterIntegrity(SubstationComponent subs, GasMixture mixture)
    {
        if (subs.InitialNitrogenBoosterMoles <= 0f)
            return 0f;

        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var usableMoles = initialN2;
        return 100 * usableMoles / subs.InitialNitrogenBoosterMoles;
    }

    private void NitrogenBoosterChanged(EntityUid uid, SubstationComponent subs)
    {
        if (!GetNitrogenBoosterMixture(uid, out var mix))
        {
            ShutdownSubstation(uid, subs);
            subs.LastIntegrity = 0f;
            return;
        }

        var initialNitrogenBoosterMoles = 0f;
        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            initialNitrogenBoosterMoles += mix.GetMoles(i);
        }

        subs.InitialNitrogenBoosterMoles = initialNitrogenBoosterMoles;
        var nitrogenBoosterIntegrity = CheckNitrogenBoosterIntegrity(subs, mix);

        if (nitrogenBoosterIntegrity <= 0.0f)
        {
            ShutdownSubstation(uid, subs);
            subs.LastIntegrity = nitrogenBoosterIntegrity;
            return;
        }

        var newState = nitrogenBoosterIntegrity switch
        {
            < 30f => SubstationIntegrityState.Bad,
            < 70f => SubstationIntegrityState.Unhealthy,
            _ => SubstationIntegrityState.Healthy,
        };
        ChangeState(uid, newState, subs);

        subs.LastIntegrity = nitrogenBoosterIntegrity;
    }

    private void ShutdownSubstation(EntityUid uid, SubstationComponent subs)
    {
        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            return;

        subs.LastIntegrity = 0.0f;
        battery.Enabled = false;
        battery.CanCharge = false;
        battery.CanDischarge = false;

        if (HasComp<ExaminableBatteryComponent>(uid))
            RemComp<ExaminableBatteryComponent>(uid);

        ChangeState(uid, SubstationIntegrityState.Bad, subs);
    }

    private void OnRejuvenate(EntityUid uid, SubstationComponent subs, RejuvenateEvent args)
    {
        subs.LastIntegrity = 100.0f;
        ChangeState(uid, SubstationIntegrityState.Healthy, subs);

        if (GetNitrogenBoosterMixture(uid, out var mix))
        {
            mix.SetMoles(Gas.Nitrogen, 1.025689525f);
        }
    }

    private void RestoreSubstation(EntityUid uid, SubstationComponent subs)
    {
        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var battery))
            return;

        battery.Enabled = true;
        battery.CanCharge = true;
        battery.CanDischarge = true;

        if (!HasComp<ExaminableBatteryComponent>(uid))
            AddComp<ExaminableBatteryComponent>(uid);
    }

    private void ChangeState(EntityUid uid, SubstationIntegrityState state, SubstationComponent? subs = null)
    {
        if (!_lightSystem.TryGetLight(uid, out var light))
            return;

        if (!Resolve(uid, ref subs, ref light, false))
            return;

        if (subs.State == state)
            return;

        if (state == SubstationIntegrityState.Healthy)
        {
            if (subs.State == SubstationIntegrityState.Bad)
            {
                RestoreSubstation(uid, subs);
            }
            _lightSystem.SetColor(uid, new Color(61, 139, 59), light);
        }
        else if (state == SubstationIntegrityState.Unhealthy)
        {
            if (subs.State == SubstationIntegrityState.Bad)
            {
                RestoreSubstation(uid, subs);
            }
            _lightSystem.SetColor(uid, Color.Yellow, light);
        }
        else
        {
            _lightSystem.SetColor(uid, Color.Red, light);
        }

        subs.State = state;
        UpdateAppearance(uid, subs.State);
    }

    private void UpdateAppearance(EntityUid uid, SubstationIntegrityState subsState)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearanceSystem.SetData(uid, SubstationVisuals.Screen, subsState, appearance);
    }

    private void OnAnalyzed(EntityUid uid, SubstationComponent slot, GasAnalyzerScanEvent args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return;

        if (!containers.TryGetContainer(slot.NitrogenBoosterSlotId, out var container))
            return;

        if (!slot.MaintenanceDoorOpen)
            return;

        if (container.ContainedEntities.Count > 0)
        {
            if (container.ContainedEntities.Count > 0 && container.ContainedEntities[0] != null)
            {
                var gasTankComponent = Comp<GasTankComponent>(container.ContainedEntities[0]);
                if (gasTankComponent != null)
                {
                    args.GasMixtures = new List<(string, GasMixture?)>
                    {
                        (Name(uid), gasTankComponent.Air)
                    };
                }
            }
        }
    }

    private bool GetNitrogenBoosterMixture(EntityUid uid, [NotNullWhen(true)] out GasMixture? mix)
    {
        mix = null;

        if (!TryComp<SubstationComponent>(uid, out var subs) || !TryComp<ContainerManagerComponent>(uid, out var containers))
            return false;

        if (!containers.TryGetContainer(subs.NitrogenBoosterSlotId, out var container))
            return false;

        if (container.ContainedEntities.Count > 0)
        {
            var gasTank = Comp<GasTankComponent>(container.ContainedEntities[0]);
            mix = gasTank.Air;
            return true;
        }

        return false;
    }

    private void OnNitrogenBoosterInsertAttempt(EntityUid uid, SubstationComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.NitrogenBoosterSlotId)
            return;

        if (!TryComp<WiresPanelComponent>(uid, out var panel))
        {
            args.Cancel();
            return;
        }

        if (component.AllowInsert)
        {
            component.AllowInsert = false;
            return;
        }

        if (!panel.Open)
        {
            args.Cancel();
        }
    }

    private void OnNitrogenBoosterRemoveAttempt(EntityUid uid, SubstationComponent component, ContainerIsRemovingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.NitrogenBoosterSlotId)
            return;

        if (!TryComp<WiresPanelComponent>(uid, out var panel))
            return;

        if (!panel.Open)
        {
            args.Cancel();
        }
    }

    private void OnNitrogenBoosterInserted(EntityUid uid, SubstationComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.NitrogenBoosterSlotId)
            return;

        NitrogenBoosterChanged(uid, component);
    }

    private void OnNitrogenBoosterRemoved(EntityUid uid, SubstationComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.NitrogenBoosterSlotId)
            return;

        NitrogenBoosterChanged(uid, component);
    }
}
