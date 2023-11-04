using Content.Server.Power.Components;
using Content.Shared.Access.Systems;
using Robust.Server.GameObjects;
using Content.Shared.Rejuvenate;
using Robust.Shared.Timing; 
using Content.Shared.Examine;


namespace Content.Server.Power.EntitySystems;

public sealed class SubstationSystem : EntitySystem 
{

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private bool _substationDecayEnabled = true;
    private const int _defaultSubstationDecayTimeout = 300; //5 minute
    private float _substationDecayCoeficient = 300000;
    private float _substationDecayTimer;

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
                var decay = battery.CurrentSupply * deltaTime / _substationDecayCoeficient;
                subs.Integrity -= decay;

                if(subs.Integrity <= 0.0f) 
                {
                    ShutdownSubstation(uid, subs, battery);
                    _substationDecayTimer = _defaultSubstationDecayTimeout;
                    _substationDecayEnabled = false;
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
        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        if(battery == null)
            return;
        subs.Integrity = 100.0f;
        battery.Enabled = true;
        battery.CanCharge = true;
        battery.CanDischarge = true;
        AddComp<ExaminableBatteryComponent>(uid);
    }

}