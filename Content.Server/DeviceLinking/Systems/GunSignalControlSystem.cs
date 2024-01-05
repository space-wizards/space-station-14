using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors;
using JetBrains.Annotations;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class GunSignalControlSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunSignalControlComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GunSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<GunSignalControlComponent> gunControl, ref ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(gunControl, gunControl.Comp.ShootPort);
    }

    private void OnSignalReceived(Entity<GunSignalControlComponent> gunControl, ref SignalReceivedEvent args)
    {
        if (!TryComp<GunComponent>(gunControl, out var gun))
            return;

        var targetPos = new EntityCoordinates(gunControl, new Vector2(0, -1));
        _gun.AttemptShoot(null, gunControl, gun, targetPos, false);
    }
}
