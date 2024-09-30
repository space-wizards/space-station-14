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
    private float _substationDecayCoeficient;
    private float _substationDecayTimer;
    private float _substationLightBlinkInterval = 1f; //1 second
    private float _substationLightBlinkTimer = 1f;
    private bool _substationLightBlinkState = true;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        _substationDecayEnabled = _cfg.GetCVar(CCVars.SubstationDecayEnabled);
        _substationDecayTimeout = _cfg.GetCVar(CCVars.SubstationDecayTimeout);
        _substationDecayCoeficient = _cfg.GetCVar(CCVars.SubstationDecayCoefficient);

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
                args.PushMarkup(
                    Loc.GetString("substation-component-examine-no-nitrogenbooster"));
                return;
            }
            else
            {
                var integrity = CheckNitrogenBoosterIntegrity(component, mix);
                if (integrity > 0.0f)
                {
                    var integrityPercentRounded = (int)integrity;
                    args.PushMarkup(
                        Loc.GetString(
                            "substation-component-examine-integrity",
                            ("percent", integrityPercentRounded),
                            ("markupPercentColor", "green")
                        ));
                }
                else
                {
                    args.PushMarkup(
                        Loc.GetString("substation-component-examine-malfunction"));
                }
            }
        }
    }

    public override void Update(float deltaTime)
    {

        base.Update(deltaTime);

        _substationLightBlinkTimer -= deltaTime;
        if (_substationLightBlinkTimer <= 0f)
        {
            _substationLightBlinkTimer = _substationLightBlinkInterval;
            _substationLightBlinkState = !_substationLightBlinkState;

            var lightquery = EntityQueryEnumerator<SubstationComponent>();
            while (lightquery.MoveNext(out var uid, out var subs))
            {
                if (subs.State == SubstationIntegrityState.Healthy)
                    continue;

                if (!_lightSystem.TryGetLight(uid, out var shlight))
                    return;

                if (_substationLightBlinkState)
                    _sharedLightSystem.SetEnergy(uid, 1.6f, shlight);
                else
                    _sharedLightSystem.SetEnergy(uid, 1f, shlight);
            }
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

    private void ConsumeNitrogenBoosterGas(float deltaTime, float scalar, SubstationComponent subs, PowerNetworkBatteryComponent battery, GasMixture mixture)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);

        var molesConsumed = (subs.InitialNitrogenBoosterMoles * battery.CurrentSupply * deltaTime) / (_substationDecayCoeficient * scalar);

        var minimumReaction = Math.Abs(initialN2) * molesConsumed / 2;

        mixture.AdjustMoles(Gas.Nitrogen, -minimumReaction);
        mixture.AdjustMoles(Gas.NitrousOxide, minimumReaction);
    }

    private float CheckNitrogenBoosterIntegrity(SubstationComponent subs, GasMixture mixture)
    {

        if (subs.InitialNitrogenBoosterMoles <= 0f)
            return 0f;

        var initialN2 = mixture.GetMoles(Gas.Nitrogen);

        var usableMoles = (initialN2);
        //return in percentage points;
        return 100 * usableMoles / (subs.InitialNitrogenBoosterMoles);
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

        var NitrogenBoosterIntegrity = CheckNitrogenBoosterIntegrity(subs, mix);

        if (NitrogenBoosterIntegrity <= 0.0f)
        {
            ShutdownSubstation(uid, subs);
            subs.LastIntegrity = NitrogenBoosterIntegrity;
            return;
        }
        if (NitrogenBoosterIntegrity < 30f)
        {
            ChangeState(uid, SubstationIntegrityState.Bad, subs);
        }
        else if (NitrogenBoosterIntegrity < 70f)
        {
            ChangeState(uid, SubstationIntegrityState.Unhealthy, subs);
        }
        else
        {
            ChangeState(uid, SubstationIntegrityState.Healthy, subs);
        }
        subs.LastIntegrity = NitrogenBoosterIntegrity;
    }

    private void ShutdownSubstation(EntityUid uid, SubstationComponent subs)
    {
        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        if (battery == null)
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
        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        if (battery == null)
            return;
        battery.Enabled = true;
        battery.CanCharge = true;
        battery.CanDischarge = true;

        if (!HasComp<ExaminableBatteryComponent>(uid))
            AddComp<ExaminableBatteryComponent>(uid);
    }

    private void ChangeState(EntityUid uid, SubstationIntegrityState state, SubstationComponent? subs=null)
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

        if (container.ContainedEntities.Count > 0)
        {
            args.GasMixtures = new Dictionary<string, GasMixture?> { {Name(uid), Comp<GasTankComponent>(container.ContainedEntities[0]).Air} };
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

        //for when the substation is initialized.
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
