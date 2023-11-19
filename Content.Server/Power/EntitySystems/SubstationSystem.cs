using System.Diagnostics.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Containers;
using Content.Server.Power.Components;
using Content.Server.Construction;
using Content.Server.Atmos.Components;
using Content.Server.Atmos;
using Content.Shared.Power.Substation;
using Content.Shared.Access.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Examine;
using Content.Shared.Atmos;

namespace Content.Server.Power.EntitySystems;

public sealed class SubstationSystem : EntitySystem 
{
    
    [Dependency] private readonly PointLightSystem _lightSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _sharedLightSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    private bool _substationDecayEnabled = true;
    private const int _defaultSubstationDecayTimeout = 300; //5 minute
    private float _substationDecayCoeficient = 300000;
    private float _substationDecayTimer;

    private float _substationLightBlinkInterval = 1f; //1 second
    private float _substationLightBlinkTimer = 1f;
    private bool _substationLightBlinkState = true;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SubstationComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SubstationComponent, UpgradeExamineEvent>(OnFuseLifetimeUpgradeExamine);
        SubscribeLocalEvent<SubstationComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SubstationComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<SubstationFuseSlotComponent, GasAnalyzerScanEvent>(OnAnalyzed);
        SubscribeLocalEvent<SubstationComponent, SubstationFuseChangedEvent>(OnFuseChanged);

    }

    private void OnComponentInit(EntityUid uid, SubstationComponent component, ComponentInit args) 
    {
        if(component.State == SubstationIntegrityState.Bad)
        {
            TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
            if(battery == null)
                return;
    
            component.LastIntegrity = 0.0f;
            battery.Enabled = false;
            battery.CanCharge = false;
            battery.CanDischarge = false;
            if(HasComp<ExaminableBatteryComponent>(uid))
                RemComp<ExaminableBatteryComponent>(uid);

            _lightSystem.TryGetLight(uid, out var light);
            if(light == null)
                return;
            
            _lightSystem.SetColor(uid, Color.Red, light);
            UpdateAppearance(uid, component.State);
        }
    }

    private void OnExamine(EntityUid uid, SubstationComponent component, ExaminedEvent args) 
    {
        if(args.IsInDetailsRange)
        {
            if(!GetFuseMixture(uid, out var mix))
            {
                args.PushMarkup(
                    Loc.GetString("substation-component-examine-no-fuse"));
                return;
            }
            else
            {
                var integrity = CheckFuseIntegrity(component, mix);
                if(integrity > 0.0f)
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

    private void OnFuseLifetimeUpgradeExamine(EntityUid uid, SubstationComponent component, UpgradeExamineEvent args)
    {
        TryComp<UpgradePowerSupplyRampingComponent>(uid, out var upgrade);
        if(upgrade == null)
            return;
        
        if(upgrade.ActualScalar < 3)
            args.AddPercentageUpgrade("upgrade-fuse-lifetime", upgrade.ActualScalar);
        else
            args.AddMaxUpgrade("upgrade-fuse-lifetime");
    }

    public override void Update(float deltaTime)
    {

        base.Update(deltaTime);

        _substationLightBlinkTimer -= deltaTime;
        if(_substationLightBlinkTimer <= 0f)
        {
            _substationLightBlinkTimer = _substationLightBlinkInterval;
            _substationLightBlinkState = !_substationLightBlinkState;

            var lightquery = EntityQueryEnumerator<SubstationComponent>();
              while(lightquery.MoveNext(out var uid, out var subs))
            {
                if(subs.State == SubstationIntegrityState.Healthy)
                    continue;
                
                if(!_lightSystem.TryGetLight(uid, out var shlight))
                    return;

                if(_substationLightBlinkState)
                    _sharedLightSystem.SetEnergy(uid, 1.6f, shlight);
                else
                    _sharedLightSystem.SetEnergy(uid, 1f, shlight);
            }
        }

        if(!_substationDecayEnabled)
        {
            _substationDecayTimer -= deltaTime;
            if(_substationDecayTimer <= 0.0f)
            {
                _substationDecayTimer = 0.0f;
                _substationDecayEnabled = true;
            }
            return;
        }

        var query = EntityQueryEnumerator<SubstationComponent, PowerNetworkBatteryComponent, UpgradePowerSupplyRampingComponent>();
        while(query.MoveNext(out var uid, out var subs, out var battery, out var upgrade))
        {
            
            if(!GetFuseMixture(uid, out var fuse))
                continue;

            if(subs.DecayEnabled && subs.LastIntegrity >= 0.0f && upgrade.ActualScalar < 3f)
            {
                ConsumeFuseGas(deltaTime, upgrade.ActualScalar, subs, battery, fuse);
                var fuseIntegrity = CheckFuseIntegrity(subs, fuse);

                if(fuseIntegrity <= 0.0f)
                {
                    ShutdownSubstation(uid, subs);
                    _substationDecayTimer = _defaultSubstationDecayTimeout;
                    _substationDecayEnabled = false;

                    subs.LastIntegrity = fuseIntegrity;
                    continue;
                }

                if(fuseIntegrity < 30f && subs.LastIntegrity >= 30f)
                {
                    ChangeState(uid, SubstationIntegrityState.Bad, subs);
                }
                else if(fuseIntegrity < 70f && subs.LastIntegrity >= 70f)
                {
                    ChangeState(uid, SubstationIntegrityState.Unhealthy, subs);
                }

                subs.LastIntegrity = fuseIntegrity;
            }
        }
    }

    private void ConsumeFuseGas(float deltaTime, float scalar, SubstationComponent subs, PowerNetworkBatteryComponent battery, GasMixture mixture)
    {
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialPlasma = mixture.GetMoles(Gas.Plasma);

        var molesConsumed = (subs.InitialFuseMoles * battery.CurrentSupply * deltaTime) / (_substationDecayCoeficient * scalar);
        
        var minimumReaction = Math.Min(initialN2, initialPlasma) * molesConsumed / 2;

        mixture.AdjustMoles(Gas.Nitrogen, -minimumReaction);
        mixture.AdjustMoles(Gas.Plasma, -minimumReaction);
        mixture.AdjustMoles(Gas.NitrousOxide, minimumReaction*2);
    }

    private float CheckFuseIntegrity(SubstationComponent subs, GasMixture mixture)
    {

        if(subs.InitialFuseMoles <= 0f)
            return 0f;

        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialPlasma = mixture.GetMoles(Gas.Plasma);

        var usableMoles = Math.Min(initialN2, initialPlasma);
        //return in percentage points;
        return 100 * usableMoles / (subs.InitialFuseMoles / 2);
    }

    private void OnFuseChanged(EntityUid uid, SubstationComponent subs, SubstationFuseChangedEvent args)
    {
        if(!GetFuseMixture(uid, out var mix))
        {
            ShutdownSubstation(uid, subs);
            subs.LastIntegrity = 0f;
            return;
        }
        
        var initialFuseMoles = 0f;
        for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            initialFuseMoles += mix.GetMoles(i);
        }

        subs.InitialFuseMoles = initialFuseMoles;

        var fuseIntegrity = CheckFuseIntegrity(subs, mix);

        if(fuseIntegrity <= 0.0f)
        {
            ShutdownSubstation(uid, subs);
            subs.LastIntegrity = fuseIntegrity;
            return;
        }
        if(fuseIntegrity < 30f)
        {
            ChangeState(uid, SubstationIntegrityState.Bad, subs);
        }
        else if(fuseIntegrity < 70f)
        {
            ChangeState(uid, SubstationIntegrityState.Unhealthy, subs);
        }
        else
        {
            ChangeState(uid, SubstationIntegrityState.Healthy, subs);
        }
        subs.LastIntegrity = fuseIntegrity;
    }

    private void ShutdownSubstation(EntityUid uid, SubstationComponent subs)
    {
        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        if(battery == null)
            return;

        subs.LastIntegrity = 0.0f;
        battery.Enabled = false;
        battery.CanCharge = false;
        battery.CanDischarge = false;
        if(HasComp<ExaminableBatteryComponent>(uid))
            RemComp<ExaminableBatteryComponent>(uid);

        ChangeState(uid, SubstationIntegrityState.Bad, subs);
    }

    private void OnRejuvenate(EntityUid uid, SubstationComponent subs, RejuvenateEvent args)
    {

        subs.LastIntegrity = 100.0f;

        ChangeState(uid, SubstationIntegrityState.Healthy, subs);

        if(GetFuseMixture(uid, out var mix))
        {
            mix.SetMoles(Gas.Nitrogen, 1.025689525f);
            mix.SetMoles(Gas.Plasma, 1.025689525f);
        }
    }

    private void RestoreSubstation(EntityUid uid, SubstationComponent subs)
    {
        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        if(battery == null)
            return;
        battery.Enabled = true;
        battery.CanCharge = true;
        battery.CanDischarge = true;

        if(!HasComp<ExaminableBatteryComponent>(uid))
            AddComp<ExaminableBatteryComponent>(uid);
    }

    private void ChangeState(EntityUid uid, SubstationIntegrityState state, SubstationComponent? subs=null)
    {

        if(!_lightSystem.TryGetLight(uid, out var light))
            return;

        if(!Resolve(uid, ref subs, ref light, false))
            return;

        if(subs.State == state)
            return;

        if(state == SubstationIntegrityState.Healthy)
        {
            if(subs.State == SubstationIntegrityState.Bad)
            {
                RestoreSubstation(uid, subs);
            }
            _lightSystem.SetColor(uid, new Color(0x3d, 0xb8, 0x3b), light);
        }
        else if(subs.State == SubstationIntegrityState.Unhealthy)
        {
            if(subs.State == SubstationIntegrityState.Bad)
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
        if(!TryComp<AppearanceComponent>(uid, out var appearance))
            return;
        _appearanceSystem.SetData(uid, SubstationVisuals.Screen, subsState, appearance);
    }

    private void OnAnalyzed(EntityUid uid, SubstationFuseSlotComponent slot, GasAnalyzerScanEvent args)
    {
        if(!TryComp<ContainerManagerComponent>(uid, out var containers))
            return;

        if(!containers.TryGetContainer(slot.FuseSlotId, out var container))
            return;

        if(container.ContainedEntities.Count > 0)
        {
            args.GasMixtures = new Dictionary<string, GasMixture?> { {Name(uid), Comp<GasTankComponent>(container.ContainedEntities[0]).Air} };
        }
    }

    private bool GetFuseMixture(EntityUid uid, [NotNullWhen(true)] out GasMixture? mix)
    {
        mix = null;

        if(!TryComp<SubstationFuseSlotComponent>(uid, out var slot) || !TryComp<ContainerManagerComponent>(uid, out var containers))
            return false;

        if(!containers.TryGetContainer(slot.FuseSlotId, out var container))
            return false;
        
        if(container.ContainedEntities.Count > 0)
        {
            var gasTank = Comp<GasTankComponent>(container.ContainedEntities[0]);
            mix = gasTank.Air;
            return true;
        }
        
        return false;
    }

}