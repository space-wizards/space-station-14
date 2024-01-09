using Content.Server.DeviceLinking.Components;
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
        _signalSystem.EnsureSinkPorts(gunControl, gunControl.Comp.TriggerPort, gunControl.Comp.OnPort, gunControl.Comp.OffPort);
    }

    private void OnSignalReceived(Entity<GunSignalControlComponent> gunControl, ref SignalReceivedEvent args)
    {
        if (args.Port == gunControl.Comp.TriggerPort)
        {
            Fire(gunControl);
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
    private void Fire(EntityUid uid)
    {
        if (!TryComp<GunComponent>(uid, out var gun))
            return;

        var targetPos = new EntityCoordinates(uid, new Vector2(0, -1));
        _gun.AttemptShoot(null, uid, gun, targetPos, false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GunSignalControlComponent, GunComponent>();
        while (query.MoveNext(out var uid, out var gunControl, out var gun))
        {
            if (!gunControl.Enabled)
                continue;

            gunControl.AccumulatedFrame += frameTime;

            if (gunControl.AccumulatedFrame > (1 / gun.FireRate))
            {
                Fire(uid);
                gunControl.AccumulatedFrame = 0f;
            }
        }
    }
}
