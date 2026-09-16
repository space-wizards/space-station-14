using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class GunSignalControlSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    [Dependency] private EntityQuery<GunComponent> _gunQuery = default!;
    [Dependency] private EntityQuery<AutoShootGunComponent> _gunAutoQuery = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<GunSignalControlComponent> gunControl, ref MapInitEvent args)
    {
        _signalSystem.EnsureSinkPorts(gunControl, gunControl.Comp.TriggerPort, gunControl.Comp.TogglePort, gunControl.Comp.OnPort, gunControl.Comp.OffPort);
    }

    [SubscribeLocalEvent]
    private void OnSignalReceived(Entity<GunSignalControlComponent> gunControl, ref SignalReceivedEvent args)
    {
        if (!_gunQuery.TryComp(gunControl, out var gun))
            return;

        if (args.Port == gunControl.Comp.TriggerPort)
            _gun.AttemptShoot((gunControl, gun));

        if (!_gunAutoQuery.TryComp(gunControl, out var autoShoot))
            return;

        if (args.Port == gunControl.Comp.TogglePort)
            _gun.SetEnabled((gunControl, autoShoot), !autoShoot.Enabled);

        if (args.Port == gunControl.Comp.OnPort)
            _gun.SetEnabled((gunControl, autoShoot), true);

        if (args.Port == gunControl.Comp.OffPort)
            _gun.SetEnabled((gunControl, autoShoot), false);
    }
}
