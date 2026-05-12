using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.SuitSensors;

public abstract class SharedSuitSensorSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSystem _stationSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<SuitSensorComponent> _sensorQuery;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuitSensorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<SuitSensorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SuitSensorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<SuitSensorComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SuitSensorComponent, EmpDisabledRemovedEvent>(OnEmpFinished);
        SubscribeLocalEvent<SuitSensorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SuitSensorComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<SuitSensorComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SuitSensorComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<SuitSensorComponent, SuitSensorChangeDoAfterEvent>(OnSuitSensorDoAfter);

        _sensorQuery = GetEntityQuery<SuitSensorComponent>();
    }

    /// <summary>
    /// Checks whether the sensor is assigned to a station or not
    /// and tries to assign an unassigned sensor to a station if it's currently on a grid.
    /// </summary>
    /// <returns>True if the sensor is assigned to a station or assigning it was successful. False otherwise.</returns>
    public bool CheckSensorAssignedStation(Entity<SuitSensorComponent> sensor)
    {
        if (!sensor.Comp.StationId.HasValue && Transform(sensor.Owner).GridUid == null)
            return false;

        sensor.Comp.StationId = _stationSystem.GetOwningStation(sensor.Owner);
        Dirty(sensor);
        return sensor.Comp.StationId.HasValue;
    }

    private void OnMapInit(Entity<SuitSensorComponent> ent, ref MapInitEvent args)
    {
        // Fallback
        ent.Comp.StationId ??= _stationSystem.GetOwningStation(ent.Owner);

        // generate random mode
        if (ent.Comp.RandomMode)
        {
            //make the sensor mode favor higher levels, except coords.
            var modesDist = new[]
            {
                SuitSensorMode.SensorOff,
                SuitSensorMode.SensorBinary, SuitSensorMode.SensorBinary,
                SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals,
                SuitSensorMode.SensorCords, SuitSensorMode.SensorCords
            };
            ent.Comp.Mode = _random.Pick(modesDist);
        }

        ent.Comp.NextUpdate = _timing.CurTime;
        Dirty(ent);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        // If the player spawns in arrivals then the grid underneath them may not be appropriate.
        // in which case we'll just use the station spawn code told us they are attached to and set all of their
        // sensors.
        RecursiveSensor(ev.Mob, ev.Station);
    }

    private void RecursiveSensor(EntityUid uid, EntityUid stationUid)
    {
        var xform = Transform(uid);
        var enumerator = xform.ChildEnumerator;

        while (enumerator.MoveNext(out var child))
        {
            if (_sensorQuery.TryComp(child, out var sensor))
            {
                sensor.StationId = stationUid;
                Dirty(child, sensor);
            }

            RecursiveSensor(child, stationUid);
        }
    }

    private void OnEquipped(Entity<SuitSensorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.User = args.Wearer;
        Dirty(ent);
    }

    private void OnUnequipped(Entity<SuitSensorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
        Dirty(ent);
    }

    private void OnEmpPulse(Entity<SuitSensorComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        ent.Comp.PreviousMode = ent.Comp.Mode;
        SetSensor(ent.AsNullable(), SuitSensorMode.SensorOff, null);

        ent.Comp.PreviousControlsLocked = ent.Comp.ControlsLocked;
        ent.Comp.ControlsLocked = true;
        // SetSensor already calls Dirty
    }

    private void OnEmpFinished(Entity<SuitSensorComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        SetSensor(ent.AsNullable(), ent.Comp.PreviousMode, null);
        ent.Comp.ControlsLocked = ent.Comp.PreviousControlsLocked;
    }

    private void OnExamine(Entity<SuitSensorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string msg;
        switch (ent.Comp.Mode)
        {
            case SuitSensorMode.SensorOff:
                msg = "suit-sensor-examine-off";
                break;
            case SuitSensorMode.SensorBinary:
                msg = "suit-sensor-examine-binary";
                break;
            case SuitSensorMode.SensorVitals:
                msg = "suit-sensor-examine-vitals";
                break;
            case SuitSensorMode.SensorCords:
                msg = "suit-sensor-examine-cords";
                break;
            default:
                return;
        }

        args.PushMarkup(Loc.GetString(msg));
    }

    private void OnVerb(Entity<SuitSensorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        // check if user can change sensor
        if (ent.Comp.ControlsLocked)
            return;

        // standard interaction checks
        if (!args.CanInteract || args.Hands == null)
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
            return;

        // check if target is incapacitated (cuffed, dead, etc)
        if (ent.Comp.User != null && args.User != ent.Comp.User && _actionBlocker.CanInteract(ent.Comp.User.Value, null))
            return;

        args.Verbs.UnionWith(new[]
        {
            CreateVerb(ent, args.User, SuitSensorMode.SensorOff),
            CreateVerb(ent, args.User, SuitSensorMode.SensorBinary),
            CreateVerb(ent, args.User, SuitSensorMode.SensorVitals),
            CreateVerb(ent, args.User, SuitSensorMode.SensorCords)
        });
    }

    private void OnInsert(Entity<SuitSensorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = args.Container.Owner;
        Dirty(ent);
    }

    private void OnRemove(Entity<SuitSensorComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = null;
        Dirty(ent);
    }

    private Verb CreateVerb(Entity<SuitSensorComponent> ent, EntityUid userUid, SuitSensorMode mode)
    {
        return new Verb()
        {
            Text = GetModeName(mode),
            Disabled = ent.Comp.Mode == mode,
            Priority = -(int)mode, // sort them in descending order
            Category = VerbCategory.SetSensor,
            Act = () => TrySetSensor(ent.AsNullable(), mode, userUid)
        };
    }

    public string GetModeName(SuitSensorMode mode)
    {
        string name;
        switch (mode)
        {
            case SuitSensorMode.SensorOff:
                name = "suit-sensor-mode-off";
                break;
            case SuitSensorMode.SensorBinary:
                name = "suit-sensor-mode-binary";
                break;
            case SuitSensorMode.SensorVitals:
                name = "suit-sensor-mode-vitals";
                break;
            case SuitSensorMode.SensorCords:
                name = "suit-sensor-mode-cords";
                break;
            default:
                return "";
        }

        return Loc.GetString(name);
    }

    /// <summary>
    /// Attempts to set <see cref="SuitSensorComponent"/> mode of the entity to the selected in params.
    /// Works instantly if the user is the player wearing the sensors and will start a DoAfter otherwise.
    /// </summary>
    /// <param name="sensors">Entity and its component that should be changed.</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">userUid, when not equal to the <see cref="SuitSensorComponent.User"/>, creates doafter</param>
    public bool TrySetSensor(Entity<SuitSensorComponent?> sensors, SuitSensorMode mode, EntityUid userUid)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return false;

        if (sensors.Comp.User == null || userUid == sensors.Comp.User)
            SetSensor(sensors, mode, userUid);
        else
        {
            var doAfterEvent = new SuitSensorChangeDoAfterEvent(mode);
            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, sensors.Comp.SensorsTime, doAfterEvent, sensors)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }
        return true;
    }

    private void OnSuitSensorDoAfter(Entity<SuitSensorComponent> sensors, ref SuitSensorChangeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        SetSensor(sensors.AsNullable(), args.Mode, args.User);
    }

    /// <summary>
    /// Sets mode of the <see cref="SuitSensorComponent"/> of the chosen entity.
    /// Makes popup when <param name="userUid"> not null
    /// </summary>
    /// <param name="sensors">Entity and it's component that should be changed</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">uid, required for the popup</param>
    public void SetSensor(Entity<SuitSensorComponent?> sensors, SuitSensorMode mode, EntityUid? userUid = null)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return;

        sensors.Comp.Mode = mode;
        Dirty(sensors);

        if (userUid != null)
        {
            var msg = Loc.GetString("suit-sensor-mode-state", ("mode", GetModeName(mode)));
            _popupSystem.PopupClient(msg, sensors, userUid.Value);
        }
    }

    /// <summary>
    /// Set all suit sensors on the equipment someone is wearing to the specified mode.
    /// </summary>
    public void SetAllSensors(EntityUid target, SuitSensorMode mode, SlotFlags slots = SlotFlags.All)
    {
        // iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(target, slots);
        while (slotEnumerator.NextItem(out var item, out _))
        {
            if (TryComp<SuitSensorComponent>(item, out var sensorComp))
                SetSensor((item, sensorComp), mode);
        }
    }

    /// <summary>
    /// Attempts to get full <see cref="SuitSensorStatus"/> from the <see cref="SuitSensorComponent"/>
    /// </summary>
    /// <param name="uid">Entity to get status</param>
    /// <returns>Full <see cref="SuitSensorStatus"/> of the chosen uid</returns>
    public SuitSensorStatus? GetSensorState(Entity<SuitSensorComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return null;

        var sensor = ent.Comp1;
        var transform = ent.Comp2;

        // check if sensor is enabled and worn by user
        if (sensor.Mode == SuitSensorMode.SensorOff || sensor.User == null || !HasComp<MobStateComponent>(sensor.User) || transform.GridUid == null)
            return null;

        // try to get mobs id from ID slot
        var userName = Loc.GetString("suit-sensor-component-unknown-name");
        var userJob = Loc.GetString("suit-sensor-component-unknown-job");
        var userJobIcon = "JobIconNoId";
        var userJobDepartments = new List<string>();

        if (_idCardSystem.TryFindIdCard(sensor.User.Value, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.LocalizedJobTitle != null)
                userJob = card.Comp.LocalizedJobTitle;
            userJobIcon = card.Comp.JobIcon;

            foreach (var department in card.Comp.JobDepartments)
                userJobDepartments.Add(Loc.GetString(_proto.Index(department).Name));
        }

        // get health mob state
        var isAlive = false;
        if (TryComp(sensor.User.Value, out MobStateComponent? mobState))
            isAlive = !_mobStateSystem.IsDead(sensor.User.Value, mobState);

        // get mob total damage
        var totalDamage = 0;
        if (TryComp<DamageableComponent>(sensor.User.Value, out var damageable))
            totalDamage = damageable.TotalDamage.Int();

        // Get mob total damage crit threshold
        int? totalDamageThreshold = null;
        if (_mobThresholdSystem.TryGetThresholdForState(sensor.User.Value, MobState.Critical, out var critThreshold))
            totalDamageThreshold = critThreshold.Value.Int();

        // finally, form suit sensor status
        var status = new SuitSensorStatus(GetNetEntity(sensor.User.Value), GetNetEntity(ent.Owner), userName, userJob, userJobIcon, userJobDepartments);
        switch (sensor.Mode)
        {
            case SuitSensorMode.SensorBinary:
                status.IsAlive = isAlive;
                break;
            case SuitSensorMode.SensorVitals:
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                break;
            case SuitSensorMode.SensorCords:
                status.IsAlive = isAlive;
                status.TotalDamage = totalDamage;
                status.TotalDamageThreshold = totalDamageThreshold;
                EntityCoordinates coordinates;
                var xformQuery = GetEntityQuery<TransformComponent>();

                if (transform.GridUid != null)
                {
                    coordinates = new EntityCoordinates(transform.GridUid.Value,
                        Vector2.Transform(_transform.GetWorldPosition(transform, xformQuery),
                            _transform.GetInvWorldMatrix(xformQuery.GetComponent(transform.GridUid.Value), xformQuery)));
                }
                else if (transform.MapUid != null)
                {
                    coordinates = new EntityCoordinates(transform.MapUid.Value,
                        _transform.GetWorldPosition(transform, xformQuery));
                }
                else
                {
                    coordinates = EntityCoordinates.Invalid;
                }

                status.Coordinates = GetNetCoordinates(coordinates);
                break;
        }

        return status;
    }

    /// <summary>
    /// Create a device network package from the suit sensors status.
    /// </summary>
    public NetworkPayload SuitSensorToPacket(SuitSensorStatus status)
    {
        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_NAME] = status.Name,
            [SuitSensorConstants.NET_JOB] = status.Job,
            [SuitSensorConstants.NET_JOB_ICON] = status.JobIcon,
            [SuitSensorConstants.NET_JOB_DEPARTMENTS] = status.JobDepartments,
            [SuitSensorConstants.NET_IS_ALIVE] = status.IsAlive,
            [SuitSensorConstants.NET_SUIT_SENSOR_UID] = status.SuitSensorUid,
            [SuitSensorConstants.NET_OWNER_UID] = status.OwnerUid,
        };

        if (status.TotalDamage != null)
            payload.Add(SuitSensorConstants.NET_TOTAL_DAMAGE, status.TotalDamage);
        if (status.TotalDamageThreshold != null)
            payload.Add(SuitSensorConstants.NET_TOTAL_DAMAGE_THRESHOLD, status.TotalDamageThreshold);
        if (status.Coordinates != null)
            payload.Add(SuitSensorConstants.NET_COORDINATES, status.Coordinates);

        return payload;
    }

    /// <summary>
    /// Try to create the suit sensors status from the device network message.
    /// </summary>
    public SuitSensorStatus? PacketToSuitSensor(NetworkPayload payload)
    {
        // check command
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return null;
        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return null;

        // check name, job and alive
        if (!payload.TryGetValue(SuitSensorConstants.NET_NAME, out string? name)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_JOB, out string? job)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_JOB_ICON, out string? jobIcon)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_JOB_DEPARTMENTS, out List<string>? jobDepartments)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_IS_ALIVE, out bool? isAlive)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_SUIT_SENSOR_UID, out NetEntity suitSensorUid)) return null;
        if (!payload.TryGetValue(SuitSensorConstants.NET_OWNER_UID, out NetEntity ownerUid)) return null;

        // try get total damage and cords (optionals)
        payload.TryGetValue(SuitSensorConstants.NET_TOTAL_DAMAGE, out int? totalDamage);
        payload.TryGetValue(SuitSensorConstants.NET_TOTAL_DAMAGE_THRESHOLD, out int? totalDamageThreshold);
        payload.TryGetValue(SuitSensorConstants.NET_COORDINATES, out NetCoordinates? coords);

        var status = new SuitSensorStatus(ownerUid, suitSensorUid, name, job, jobIcon, jobDepartments)
        {
            IsAlive = isAlive.Value,
            TotalDamage = totalDamage,
            TotalDamageThreshold = totalDamageThreshold,
            Coordinates = coords,
        };
        return status;
    }
}
