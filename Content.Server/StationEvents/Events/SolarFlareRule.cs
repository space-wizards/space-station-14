using System.Linq;
using System.Text;
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
using Content.Shared.DeviceLinking;
using Content.Shared.Lock;
using Content.Shared.VendingMachines.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that cause a solar flare for some amount of time.
/// Various systems will malfunction, including lights, doors, air alarms, and radios.
/// </summary>
/// <remarks>
/// The event only affects entities that existed when the solar flare started.
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
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    [Dependency] private EntityQuery<HeadsetComponent> _headsetQuery;

    private static readonly AirAlarmMode[] AirAlarmModes = Enum.GetValues<AirAlarmMode>();

    private static readonly SecurityStatus[] CrimeStatuses = Enum.GetValues<SecurityStatus>()
        .Where(status => status != SecurityStatus.None)
        .ToArray();

    /// <remarks>
    /// Filter out special (boring) characters with no representation in our font.
    /// They all appear as the unknown symbol indicator.
    /// </remarks>
    private static readonly int[] ValidAnnouncementScrambleCharacters =
        [.. Enumerable.Range(32, 96), .. Enumerable.Range(160, 96)];

    protected override void Added(Entity<SolarFlareRuleComponent, GameRuleComponent> solarFlare,
        ref GameRuleAddedEvent args)
    {
        if (TryComp<StationEventComponent>(solarFlare, out var stationEvent))
        {
            var announcement = Loc.GetString("station-event-solar-flare-start-announcement");

            var scrambledAnnouncement = new StringBuilder(announcement.Length);
            for (float i = 0; i < announcement.Length; i++)
            {
                // Power of 4 gives almost no scramble in the first half, then quickly reaches 100% scramble.
                // So you can easily see it's the solar flare announcement still.
                if (RobustRandom.Prob(MathF.Pow(i / announcement.Length, 4)))
                {
                    scrambledAnnouncement.Append((char)RobustRandom.Pick(ValidAnnouncementScrambleCharacters));
                }
                else
                {
                    scrambledAnnouncement.Append(announcement[(int)i]);
                }
            }


            stationEvent.StartAnnouncement = scrambledAnnouncement.ToString();
        }

        base.Added(solarFlare, ref args);
    }

    protected override void Started(Entity<SolarFlareRuleComponent, GameRuleComponent> solarFlare,
        ref GameRuleStartedEvent args)
    {
        base.Started(solarFlare, ref args);

        var solarFlareComp = solarFlare.Comp1;

        if (solarFlareComp.ExtraChannels.Count > 0 && solarFlareComp.ExtraCount > 0)
        {
            for (var i = 0; i < solarFlareComp.ExtraCount; i++)
            {
                var channel = RobustRandom.Pick(solarFlare.Comp1.ExtraChannels);
                solarFlareComp.AffectedChannels.Add(channel);
            }
        }

        solarFlareComp.AffectedLights = Station
            .GetEntitiesWithComponentOnStation<PoweredLightComponent>(true, out var station)
            .Select(e => e.Owner)
            .ToHashSet();

        solarFlareComp.AffectedAirlocks = Station.GetEntitiesWithComponentOnStation<AirlockComponent>(true)
            .Select(e => e.Owner)
            .ToHashSet();

        solarFlareComp.AffectedAirAlarms = Station.GetEntitiesWithComponentOnStation<AirAlarmComponent>(true)
            .Select(e => e.Owner)
            .ToHashSet();

        solarFlareComp.AffectedStation = station?.Owner;

        if (solarFlareComp.AffectedStation is { } stationUid
            && TryComp<StationRecordsComponent>(stationUid, out var stationRecords))
        {
            foreach (var (recordId, general) in _stationRecords.GetRecordsOfType<GeneralStationRecord>((stationUid,
                         stationRecords)))
            {
                var key = new StationRecordKey(recordId, stationUid);
                if (_stationRecords.TryGetRecord<CriminalRecord>(key, out var criminal))
                {
                    solarFlareComp.AffectedStationRecords.Add((key, general, criminal));
                }
            }
        }

        solarFlareComp.AffectedVendingMachines = Station
            .GetEntitiesWithComponentOnStation<VendingMachineComponent>(true)
            .Select(e => e.Owner)
            .ToHashSet();

        solarFlareComp.AffectedLocks = Station.GetEntitiesWithComponentOnStation<LockComponent>(false)
            .Select(e => e.Owner)
            .ToHashSet();

        solarFlareComp.AffectedLinkSources = Station
            .GetEntitiesWithComponentOnStation<DeviceLinkSourceComponent>(false)
            .Select(e => e.Owner)
            .Where(e => _entityWhitelist.IsWhitelistFail(solarFlareComp.DeviceLinkSourceBlacklist, e))
            .ToHashSet();
    }

    protected override void ActiveTick(
        EntityUid solarFlare,
        SolarFlareRuleComponent solarFlareComp,
        GameRuleComponent gameRuleComp,
        float frameTime)
    {
        base.ActiveTick(solarFlare, solarFlareComp, gameRuleComp, frameTime);

        solarFlareComp.EffectTimer -= TimeSpan.FromSeconds(frameTime);
        if (solarFlareComp.EffectTimer > TimeSpan.Zero)
        {
            return;
        }

        solarFlareComp.EffectTimer += TimeSpan.FromSeconds(1);

        foreach (var light in solarFlareComp.AffectedLights)
        {
            if (RobustRandom.Prob(solarFlareComp.LightBreakChance)
                && TryComp(light, out PoweredLightComponent? lightComp))
            {
                _poweredLight.TryDestroyBulb(light, lightComp);
            }
        }

        foreach (var airlock in solarFlareComp.AffectedAirlocks)
        {
            var action = PickDoorAction(solarFlareComp);
            if (action is null)
            {
                continue;
            }

            if (!TryComp(airlock, out AirlockComponent? airlockComp))
            {
                continue;
            }

            switch (action)
            {
                case SolarFlareDoorAction.Toggle:
                    if (airlockComp.AutoClose)
                    {
                        _door.TryToggleDoor(airlock);
                    }

                    break;
                case SolarFlareDoorAction.Bolt:
                    if (TryComp(airlock, out DoorBoltComponent? boltComp))
                    {
                        _door.SetBoltsDown((airlock, boltComp), !boltComp.BoltsDown);
                    }

                    break;
                case SolarFlareDoorAction.EmergencyAccess:
                    _airlock.SetEmergencyAccess((airlock, airlockComp), !airlockComp.EmergencyAccess);
                    break;
                case SolarFlareDoorAction.Electrify:
                    if (TryComp(airlock, out ElectrifiedComponent? electrifiedComp))
                    {
                        _electrocution.SetElectrified((airlock, electrifiedComp), !electrifiedComp.Enabled);
                    }

                    break;
            }
        }

        foreach (var airAlarm in solarFlareComp.AffectedAirAlarms)
        {
            if (!RobustRandom.Prob(solarFlareComp.AirAlarmModeChangeChance)
                || !TryComp(airAlarm, out AirAlarmComponent? airAlarmComp))
            {
                continue;
            }

            airAlarmComp.AutoMode = false;
            _airAlarm.SetMode(airAlarm, string.Empty, RobustRandom.Pick(AirAlarmModes), false, airAlarmComp);
        }

        var totalChangeCriminalRecordChance =
            solarFlareComp.ChangeCriminalRecordChance * solarFlareComp.AffectedStationRecords.Count;
        if (solarFlareComp.AffectedStationRecords.Count >= 1
            && RobustRandom.Prob(totalChangeCriminalRecordChance))
        {
            (StationRecordKey Key, GeneralStationRecord General, CriminalRecord Criminal) target =
                RobustRandom.Pick(solarFlareComp.AffectedStationRecords);

            _criminalRecords.OverwriteStatus(
                target.Key,
                target.Criminal,
                RobustRandom.Pick(CrimeStatuses),
                null);
        }

        foreach (var vendingMachine in solarFlareComp.AffectedVendingMachines)
        {
            if (!RobustRandom.Prob(solarFlareComp.VendChance)
                || !TryComp(vendingMachine, out VendingMachineComponent? vendingMachineComp))
            {
                continue;
            }

            _vendingMachine.EjectRandom((vendingMachine, vendingMachineComp), true);
        }

        foreach (var lockable in solarFlareComp.AffectedLocks)
        {
            if (!RobustRandom.Prob(solarFlareComp.LockToggleChance)
                || !TryComp(lockable, out LockComponent? lockComp))
            {
                continue;
            }

            _lock.ToggleLock(lockable, null, lockComp);
        }

        foreach (var linkSource in solarFlareComp.AffectedLinkSources)
        {
            if (!RobustRandom.Prob(solarFlareComp.LinkPortInvokeChance)
                || !TryComp(linkSource, out DeviceLinkSourceComponent? linkSourceComp))
            {
                continue;
            }

            foreach (var port in linkSourceComp.Ports)
            {
                _deviceLink.InvokePort((linkSource, linkSourceComp), port);
            }
        }
    }

    /// <summary>
    /// Picks a random door action to perform based on the weighted chances in the solar flare component.
    /// </summary>
    /// <param name="solarFlareComp"></param>
    /// <returns>The randomly selected door action, or null if no action is selected.</returns>
    private SolarFlareDoorAction? PickDoorAction(SolarFlareRuleComponent solarFlareComp)
    {
        var totalChance = solarFlareComp.DoorToggleChance
                          + solarFlareComp.DoorBoltChance
                          + solarFlareComp.DoorEmergencyAccessChance
                          + solarFlareComp.DoorElectrifyChance;

        if (!RobustRandom.Prob(totalChance))
        {
            return null;
        }

        var roll = RobustRandom.NextFloat(totalChance);

        if (roll < solarFlareComp.DoorToggleChance)
        {
            return SolarFlareDoorAction.Toggle;
        }

        roll -= solarFlareComp.DoorToggleChance;

        if (roll < solarFlareComp.DoorBoltChance)
        {
            return SolarFlareDoorAction.Bolt;
        }

        roll -= solarFlareComp.DoorBoltChance;

        if (roll < solarFlareComp.DoorEmergencyAccessChance)
        {
            return SolarFlareDoorAction.EmergencyAccess;
        }

        return SolarFlareDoorAction.Electrify;
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
        EmergencyAccess,
        Electrify,
    }
}
