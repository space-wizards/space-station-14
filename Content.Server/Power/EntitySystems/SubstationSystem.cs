using Content.Server.Power.Components;
using Content.Shared.Access.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing; 
using Content.Shared.Examine;


namespace Content.Server.Power.EntitySystems;

public sealed class SubstationSystem : EntitySystem 
{

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private bool _substationWearEnabled = true;
    private const int _defaultSubstationWearTimeout = 300000; //5 minutes
    private float _substationWearCoeficient = 300000;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SubstationComponent, MapInitEvent>(OnSubstationInit);
        SubscribeLocalEvent<SubstationComponent, ExaminedEvent>(OnExamine);
    }

    private void OnSubstationInit(EntityUid uid, SubstationComponent component, MapInitEvent args) 
    {
        component.Enabled = true;
        component.Integrity = 100.0f;
    }

    private void OnExamine(EntityUid uid, SubstationComponent component, ExaminedEvent args) 
    {
        if (args.IsInDetailsRange)
        {
            if(component.Enabled)
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
        var query = EntityQueryEnumerator<SubstationComponent, PowerNetworkBatteryComponent>();
        while (query.MoveNext(out var uid, out var subs, out var battery))
        {
            if(subs.Integrity > 0.0f && subs.Enabled)
            {
                var wear = battery.CurrentSupply * deltaTime / _substationWearCoeficient;
                subs.Integrity -= wear;

                if(subs.Integrity <= 0.0f) 
                {
                    ShutdownSubstation(uid, subs, battery);
                    _substationWearEnabled = false;
                    //Timer.Spawn(_defaultSubstationWearTimeout, EnableSubstationWear);
                }
            }
        }
    }

    private void EnableSubstationWear() 
    {
        _substationWearEnabled = true;
    }

    private void ShutdownSubstation(EntityUid uid, SubstationComponent? subs=null, PowerNetworkBatteryComponent? battery=null)
    {
        if (!Resolve(uid, ref subs, ref battery, false))
            return;
        subs.Enabled = false;
        subs.Integrity = 0.0f;
        battery.Enabled = false;
        battery.CanCharge = false;
        battery.CanDischarge = false;
        RemComp<ExaminableBatteryComponent>(uid);
    }

}