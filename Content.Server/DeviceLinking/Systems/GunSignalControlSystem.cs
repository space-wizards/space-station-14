using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class GunSignalControlSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunSignalControlComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<GunSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<GunSignalControlComponent> gunControl, ref MapInitEvent args)
    {
        _signalSystem.EnsureSinkPorts(gunControl, gunControl.Comp.TriggerPort, gunControl.Comp.TogglePort, gunControl.Comp.OnPort, gunControl.Comp.OffPort);
    }

    private void OnSignalReceived(Entity<GunSignalControlComponent> gunControl, ref SignalReceivedEvent args)
    {
        if (!TryComp<GunComponent>(gunControl, out var gun))
            return;

        if (args.Port == gunControl.Comp.TriggerPort)
        {
            _gun.AttemptShoot(null, gunControl, gun);
        }

        if (!TryComp<AutoShootGunComponent>(gunControl, out var autoShoot))
            return;

        if (args.Port == gunControl.Comp.TogglePort)
        {
            autoShoot.Enabled = !autoShoot.Enabled;
        }
        if (args.Port == gunControl.Comp.OnPort)
        {
            autoShoot.Enabled = true;
        }
        if (args.Port == gunControl.Comp.OffPort)
        {
            autoShoot.Enabled = false;
        }
    }
}
