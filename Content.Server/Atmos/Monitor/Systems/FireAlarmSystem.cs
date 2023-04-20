using Content.Server.AlertLevel;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.AlertLevel;
using Content.Shared.Atmos.Monitor;
using Content.Shared.CCVar;
using Content.Shared.DeviceNetwork;
using Content.Shared.Interaction;
using Content.Shared.Emag.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.Monitor.Systems;

public sealed class FireAlarmSystem : EntitySystem
{
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private readonly AtmosAlarmableSystem _atmosAlarmable = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FireAlarmComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<FireAlarmComponent, DeviceListUpdateEvent>(OnDeviceListSync);
        SubscribeLocalEvent<FireAlarmComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnDeviceListSync(EntityUid uid, FireAlarmComponent component, DeviceListUpdateEvent args)
    {
        var query = GetEntityQuery<DeviceNetworkComponent>();
        foreach (var device in args.OldDevices)
        {
            if (!query.TryGetComponent(device, out var deviceNet))
            {
                continue;
            }

            _atmosDevNet.Deregister(uid, deviceNet.Address);
        }

        _atmosDevNet.Register(uid, null);
        _atmosDevNet.Sync(uid, null);
    }

    private void OnInteractHand(EntityUid uid, FireAlarmComponent component, InteractHandEvent args)
    {
        if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
            return;

        if (!_configManager.GetCVar(CCVars.FireAlarmAllAccess) && !_access.IsAllowed(args.User, args.Target))
            return;

        if (this.IsPowered(uid, EntityManager))
        {
            if (!_atmosAlarmable.TryGetHighestAlert(uid, out var alarm))
            {
                alarm = AtmosAlarmType.Normal;
            }

            if (alarm == AtmosAlarmType.Normal)
            {
                _atmosAlarmable.ForceAlert(uid, AtmosAlarmType.Danger);
            }
            else
            {
                _atmosAlarmable.ResetAllOnNetwork(uid);
            }
        }
    }

    private void OnEmagged(EntityUid uid, FireAlarmComponent component, ref GotEmaggedEvent args)
    {
        if (TryComp<AtmosAlarmableComponent>(uid, out var alarmable))
        {
            // Remove the atmos alarmable component permanently from this device.
            _atmosAlarmable.ForceAlert(uid, AtmosAlarmType.Emagged, alarmable);
            RemCompDeferred<AtmosAlarmableComponent>(uid);
        }
    }
}
