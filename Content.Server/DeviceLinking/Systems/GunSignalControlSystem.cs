using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using System.Numerics;
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
            Fire(gunControl, gun);
        }
        if (args.Port == gunControl.Comp.TogglePort)
        {
            gunControl.Comp.Enabled = !gunControl.Comp.Enabled;
        }
        if (args.Port == gunControl.Comp.OnPort)
        {
            gunControl.Comp.Enabled = true;
        }
        if (args.Port == gunControl.Comp.OffPort)
        {
            gunControl.Comp.Enabled = false;
        }
    }
    private void Fire(EntityUid uid, GunComponent gun)
    {
        var targetPos = new EntityCoordinates(uid, new Vector2(0, -1));
        _gun.AttemptShoot(null, uid, gun, targetPos);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GunSignalControlComponent, GunComponent>();
        while (query.MoveNext(out var uid, out var gunControl, out var gun))
        {
            if (!gunControl.Enabled)
                continue;

            if (gunControl.NextShootTime > _timing.CurTime)
                return;

            Fire(uid, gun);
            gunControl.NextShootTime = _timing.CurTime + TimeSpan.FromSeconds(1 / gun.FireRate);
        }
    }
}
