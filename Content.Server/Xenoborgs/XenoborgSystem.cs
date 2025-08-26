using Content.Server.DeviceNetwork.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.Robotics;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Xenoborgs.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Xenoborgs;

public sealed partial class XenoborgSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothershipCoreComponent, DestructionEventArgs>(OnDestroyed);
    }

    private void OnDestroyed(EntityUid ent, MothershipCoreComponent component, DestructionEventArgs args)
    {
        Log.Debug("mothership core destroyed");

        // if a mothership core is destroyed, it will see if there are any others
        var mothershipCoreQuery = AllEntityQuery<MothershipCoreComponent>();
        while (mothershipCoreQuery.MoveNext(out var mothershipCoreEnt, out _))
        {
            // if it finds a mothership core that is different from the one just destroyed,
            // it doesn't explode the xenoborgs
            if (mothershipCoreEnt != ent)
                return;
        }

        Log.Debug("it was the last one");

        // explode all xenoborgs
        var xenoborgQuery = AllEntityQuery<XenoborgComponent, BorgTransponderComponent, DeviceNetworkComponent>();
        while (xenoborgQuery.MoveNext(out var xenoborgEnt, out _, out var xenoborgTransponder, out var xenoborgNetwork))
        {
            var address = xenoborgNetwork.Address;

            Log.Debug($"xenoborg: {xenoborgEnt}\nadress: {address}");

            var payload = new NetworkPayload()
            {
                [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DESTROY_COMMAND
            };

            _deviceNetwork.QueuePacket(ent, address, payload);
        }
    }
}
