using System.Linq;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Light.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.VendingMachines;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that cause a solar flare for some amount of time.
/// </summary>
/// <remarks>
/// When a solar flare is active, radio communications are disabled on a random set of channels,
/// doors can randomly open and close, and lights can explode.
/// </remarks>
/// <seealso cref="SolarFlareRuleComponent"/>
public sealed partial class SolarFlareRule : StationEventSystem<SolarFlareRuleComponent>
{
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedAirlockSystem _airlock = default!;
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private AirAlarmSystem _airAlarm = default!;
    [Dependency] private CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private VendingMachineSystem _vendingMachine = default!;

    [Dependency] private EntityQuery<HeadsetComponent> _headsetQuery;

    private static readonly SolarFlareDoorAction[] SolarFlareActions = Enum.GetValues<SolarFlareDoorAction>();
    private static readonly AirAlarmMode[] AirAlarmModes = Enum.GetValues<AirAlarmMode>();
    private static readonly SecurityStatus[] CrimeStatuses = Enum.GetValues<SecurityStatus>()
        .Where(status => status != SecurityStatus.None)
        .ToArray();

    private float _effectTimer;

    protected override void Started(Entity<SolarFlareRuleComponent, GameRuleComponent> ent,
        ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var solarFlareRuleComponent = ent.Comp1;

        for (var i = 0; i < solarFlareRuleComponent.ExtraCount; i++)
        {
            var channel = RobustRandom.Pick(ent.Comp1.ExtraChannels);
            solarFlareRuleComponent.AffectedChannels.Add(channel);
        }

        solarFlareRuleComponent.AffectedLights = Station
            .GetEntitiesWithComponentOnStation<PoweredLightComponent>(true, out var station)
            .Select(e => (e.Owner, e.Comp))
            .ToHashSet();
        solarFlareRuleComponent.AffectedAirlocks = Station.GetEntitiesWithComponentOnStation<AirlockComponent>(true)
            .Select(e => (e.Owner, e.Comp))
            .ToHashSet();
        solarFlareRuleComponent.AffectedAirAlarms = Station.GetEntitiesWithComponentOnStation<AirAlarmComponent>(true)
            .Select(e => (e.Owner, e.Comp))
            .ToHashSet();

        solarFlareRuleComponent.AffectedStation = station?.Owner;

        if (solarFlareRuleComponent.AffectedStation is { } stationUid
            && TryComp<StationRecordsComponent>(stationUid, out var stationRecords))
        {
            foreach (var (recordId, general) in _stationRecords.GetRecordsOfType<GeneralStationRecord>((stationUid,
                         stationRecords)))
            {
                var key = new StationRecordKey(recordId, stationUid);
                if (_stationRecords.TryGetRecord<CriminalRecord>(key, out var criminal))
                {
                    solarFlareRuleComponent.AffectedStationRecords.Add((key, general, criminal));
                }
            }
        }

        solarFlareRuleComponent.AffectedVendingMachines = Station
            .GetEntitiesWithComponentOnStation<VendingMachineComponent>(true)
            .Select(e => (e.Owner, e.Comp))
            .ToHashSet();
    }

    protected override void ActiveTick(EntityUid uid, SolarFlareRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        _effectTimer -= frameTime;
        if (!(_effectTimer < 0))
        {
            return;
        }

        _effectTimer += 1;
        foreach (var light in component.AffectedLights)
        {
            if (RobustRandom.Prob(component.LightBreakChance))
            {
                _poweredLight.TryDestroyBulb(light.Item1, light.Item2);
            }
        }

        foreach (var airlockEnt in component.AffectedAirlocks)
        {
            if (!RobustRandom.Prob(component.DoorAffectChance))
            {
                continue;
            }

            var action = RobustRandom.Pick(SolarFlareActions);

            switch (action)
            {
                case SolarFlareDoorAction.Toggle:
                    if (airlockEnt.Item2.AutoClose)
                    {
                        _door.TryToggleDoor(airlockEnt.Item1);
                    }

                    break;
                case SolarFlareDoorAction.Bolt:
                    if (TryComp<DoorBoltComponent>(airlockEnt.Item1, out var boltComp))
                    {
                        _door.SetBoltsDown((airlockEnt.Item1, boltComp), true);
                    }

                    break;
                case SolarFlareDoorAction.EnableEmergencyAccess:
                    _airlock.SetEmergencyAccess((airlockEnt.Item1, airlockEnt.Item2), true);
                    break;
                case SolarFlareDoorAction.Electrify:
                    if (TryComp<ElectrifiedComponent>(airlockEnt.Item1, out var electrifiedComp))
                    {
                        _electrocution.SetElectrified((airlockEnt.Item1, electrifiedComp), true);
                    }

                    break;
            }
        }

        foreach (var airAlarm in component.AffectedAirAlarms)
        {
            if (!RobustRandom.Prob(component.AirAlarmModeChangeChance))
            {
                continue;
            }

            airAlarm.Item2.AutoMode = false;
            _airAlarm.SetMode(airAlarm.Item1, string.Empty, RobustRandom.Pick(AirAlarmModes), false, airAlarm.Item2);
        }

        var chance = component.ChangeCriminalRecordChance * component.AffectedStationRecords.Count;
        if (component.AffectedStationRecords.Count >= 1
            && RobustRandom.Prob(chance))
        {
            (StationRecordKey Key, GeneralStationRecord General, CriminalRecord Criminal) target =
                RobustRandom.Pick(component.AffectedStationRecords);

            _criminalRecords.OverwriteStatus(
                target.Key,
                target.Criminal,
                RobustRandom.Pick(CrimeStatuses),
                null);
        }

        foreach (var vendingMachine in component.AffectedVendingMachines)
        {
            if (!RobustRandom.Prob(component.VendChance))
            {
                continue;
            }

            _vendingMachine.EjectRandom(vendingMachine, true);
        }


    }

    [SubscribeLocalEvent]
    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var query = EntityQueryEnumerator<SolarFlareRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var flare, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive((uid, gameRule)))
                continue;

            if (!flare.AffectedChannels.Contains(args.Channel.ID))
                continue;

            if (!flare.OnlyJamHeadsets || _headsetQuery.HasComp(args.RadioReceiver) || _headsetQuery.HasComp(args.RadioSource))
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    private enum SolarFlareDoorAction
    {
        Toggle,
        Bolt,
        EnableEmergencyAccess,
        Electrify,
    }
}
