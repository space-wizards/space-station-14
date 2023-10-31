using System;
using System.Diagnostics.SymbolStore;
using System.Security.Cryptography;
using System.Security.Permissions;
using Content.Server.Power.Components;
using Content.Server.Power.Pow3r;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Power.EntitySystems;

public sealed class SubstationSystem : EntitySystem 
{

    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private bool _substationWearEnabled = true;
    private const int _defaultSubstationWearTimeout = 300000; //5 minutes
    private float _substationWearCoeficient = 1 / 300000;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SubstationComponent, MapInitEvent>(OnSubstationInit);
        SubscribeLocalEvent<SubstationComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SubstationComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnSubstationInit(EntityUid uid, SubstationComponent component, MapInitEvent args) 
    {}

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
        if(!_substationWearEnabled)
            return;
        
        var query = EntityQueryEnumerator<SubstationComponent, PowerNetworkBatteryComponent>();
        while (query.MoveNext(out var uid, out var subs, out var battery))
        {

            if(subs.Integrity > 0.0f && subs.Enabled) {
                var wear = battery.CurrentSupply * deltaTime * _substationWearCoeficient;
                subs.Integrity -= wear;

                if(subs.Integrity <= 0.0f) {
                    ShutdownSubstation(uid, subs, battery);
                    _substationWearEnabled = false;
                    Timer.Spawn(_defaultSubstationWearTimeout, EnableSubstationWear);
                }
            }
        }
    }

    private void EnableSubstationWear() 
    {
        _substationWearEnabled = true;
    }}

    private void OnEmagged(EntityUid uid, SubstationComponent comp, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        TryComp<PowerNetworkBatteryComponent>(uid, out var battery);
        ShutdownSubstation(uid, comp, battery);
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