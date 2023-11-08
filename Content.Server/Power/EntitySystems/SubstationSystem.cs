using Robust.Server.GameObjects;
using Robust.Shared.Timing; 
using Content.Server.Power.Components;
using Content.Shared.Power.Substation;
using Content.Shared.Access.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Examine;

namespace Content.Server.Power.EntitySystems;

public sealed class SubstationSystem : EntitySystem 
{

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PointLightSystem _lightSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _sharedLightSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;


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

        SubscribeLocalEvent<SubstationComponent, MapInitEvent>(OnSubstationInit);
        SubscribeLocalEvent<SubstationComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SubstationComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnSubstationInit(EntityUid uid, SubstationComponent component, MapInitEvent args) 
    {}

    private void OnExamine(EntityUid uid, SubstationComponent component, ExaminedEvent args) 
    {
        if (args.IsInDetailsRange)
        {
            if(component.Integrity > 0.0f)
            {
                var integrityPercentRounded = (int) (component.Integrity);
                args.PushMarkup(
                    Loc.GetString(
                        "substation-component-examine-integrity",
                        ("percent", integrityPercentRounded),
                        ("markupPercentColor", "green")
                    )
                );
            }
            else
            {
                args.PushMarkup(
                Loc.GetString(
                    "substation-component-examine-malfunction"
                    )
                );
            }
        }
    }

    public override void Update(float deltaTime)
    {

        _substationLightBlinkTimer -= deltaTime;
		if(_substationLightBlinkTimer <= 0f)
		{
			_substationLightBlinkTimer = _substationLightBlinkInterval;
			_substationLightBlinkState = !_substationLightBlinkState;
;
            var lightquery = EntityQueryEnumerator<SubstationComponent, PointLightComponent>();
	      	while (lightquery.MoveNext(out var uid, out var subs, out var light))
			{
                if(subs.State == SubstationIntegrityState.Healthy)
                    continue;
                
				_lightSystem.TryGetLight(uid, out var shlight);
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

        var query = EntityQueryEnumerator<SubstationComponent, PowerNetworkBatteryComponent>();
        while (query.MoveNext(out var uid, out var subs, out var battery))
        {

            if(subs.DecayEnabled && subs.Integrity >= 0.0f)
            {
                var lastIntegrity = subs.Integrity;
                var decay = battery.CurrentSupply * deltaTime / _substationDecayCoeficient;
                subs.Integrity -= decay;

                if(subs.Integrity <= 0.0f) 
                {
                    ShutdownSubstation(uid, subs, battery);
                    _substationDecayTimer = _defaultSubstationDecayTimeout;
                    _substationDecayEnabled = false;
                }

                if(subs.Integrity < 30f && lastIntegrity >= 30f)
                {
                    subs.State = SubstationIntegrityState.Bad;
				    _lightSystem.TryGetLight(uid, out var shlight);
                    ChangeLightColor(uid, subs, shlight);
                    continue;
                }
                if(subs.Integrity < 70f && lastIntegrity >= 70f)
                {
                    subs.State = SubstationIntegrityState.Unhealthy;
				    _lightSystem.TryGetLight(uid, out var shlight);
                    ChangeLightColor(uid, subs, shlight);
                }
            }
        }
    }

    private void ShutdownSubstation(EntityUid uid, SubstationComponent? subs=null, PowerNetworkBatteryComponent? battery=null)
    {
        if (!Resolve(uid, ref subs, ref battery, false))
            return;
        subs.Integrity = 0.0f;
        battery.Enabled = false;
        battery.CanCharge = false;
        battery.CanDischarge = false;
        RemComp<ExaminableBatteryComponent>(uid);
    }

    private void OnRejuvenate(EntityUid uid, SubstationComponent subs, RejuvenateEvent args)
    {

        subs.Integrity = 100.0f;
        subs.State = SubstationIntegrityState.Healthy;

        TryComp<PointLightComponent>(uid, out var light);
        ChangeLightColor(uid, subs, light);

        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        if(battery == null)
            return;
        battery.Enabled = true;
        battery.CanCharge = true;
        battery.CanDischarge = true;

        if(!HasComp<ExaminableBatteryComponent>(uid))
            AddComp<ExaminableBatteryComponent>(uid);
    }

    private void ChangeLightColor(EntityUid uid, SubstationComponent? subs=null, SharedPointLightComponent? light= null)
	{
		if (!Resolve(uid, ref subs, ref light, false))
            return;
		
		if(subs.State == SubstationIntegrityState.Healthy)
        {
			_lightSystem.SetColor(uid, new Color(0x3d, 0xb8, 0x3b), light);
        }
        else if(subs.State == SubstationIntegrityState.Unhealthy)
        {
			_lightSystem.SetColor(uid, Color.Yellow, light);
        }
        else
        {
        _lightSystem.SetColor(uid, Color.Red, light);
        }

        UpdateAppearance(uid, subs.State);
	}

    private void UpdateAppearance(EntityUid uid, SubstationIntegrityState subsState)
    {
        if(!TryComp<AppearanceComponent>(uid, out var appearance))
            return;
        _appearance.SetData(uid, SubstationVisuals.Screen, subsState, appearance);
    }
}