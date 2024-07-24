using Content.Shared.DeviceNetwork;
using Content.Shared.Emag.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Robotics;
using Content.Shared.Silicons.Borgs.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Explosion.Components;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    private void InitializeTransponder()
    {
        SubscribeLocalEvent<BorgTransponderComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BorgTransponderComponent, BorgChassisComponent, DeviceNetworkComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var chassis, out var device, out var meta))
        {
            if (comp.NextDisable is {} nextDisable && now >= nextDisable)
                DoDisable((uid, comp, chassis, meta));

            if (now < comp.NextBroadcast)
                continue;

            var charge = 0f;
            if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
                charge = battery.CurrentCharge / battery.MaxCharge;

            var hasBrain = chassis.BrainEntity != null && !comp.FakeDisabled;
            var canDisable = comp.NextDisable == null && !comp.FakeDisabling;
            var data = new CyborgControlData(
                comp.Sprite,
                comp.Name,
                meta.EntityName,
                charge,
                chassis.ModuleCount,
                hasBrain,
                canDisable);

            var payload = new NetworkPayload()
            {
                [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
                [RoboticsConsoleConstants.NET_CYBORG_DATA] = data
            };
            _deviceNetwork.QueuePacket(uid, null, payload, device: device);

            comp.NextBroadcast = now + comp.BroadcastDelay;
        }
    }

    private void DoDisable(Entity<BorgTransponderComponent, BorgChassisComponent, MetaDataComponent> ent)
    {
        ent.Comp1.NextDisable = null;
        if (ent.Comp1.FakeDisabling)
        {
            ent.Comp1.FakeDisabled = true;
            ent.Comp1.FakeDisabling = false;
            return;
        }

        if (ent.Comp2.BrainEntity is not {} brain)
            return;

        var message = Loc.GetString(ent.Comp1.DisabledPopup, ("name", Name(ent, ent.Comp3)));
        Popup.PopupEntity(message, ent);
        _container.Remove(brain, ent.Comp2.BrainContainer);
    }

    private void OnPacketReceived(Entity<BorgTransponderComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        if (command == RoboticsConsoleConstants.NET_DISABLE_COMMAND)
            Disable(ent);
        else if (command == RoboticsConsoleConstants.NET_DESTROY_COMMAND)
            Destroy(ent);
    }

    private void Disable(Entity<BorgTransponderComponent, BorgChassisComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2) || ent.Comp2.BrainEntity == null || ent.Comp1.NextDisable != null)
            return;

        // update ui immediately
        ent.Comp1.NextBroadcast = _timing.CurTime;

        // pretend the borg is being disabled forever now
        if (CheckEmagged(ent, "disabled"))
            ent.Comp1.FakeDisabling = true;
        else
            Popup.PopupEntity(Loc.GetString(ent.Comp1.DisablingPopup), ent);

        ent.Comp1.NextDisable = _timing.CurTime + ent.Comp1.DisableDelay;
    }

    private void Destroy(Entity<BorgTransponderComponent> ent)
    {
        // this is stealthy until someone realises you havent exploded
        if (CheckEmagged(ent, "destroyed"))
        {
            // prevent reappearing on the console a few seconds later
            RemComp<BorgTransponderComponent>(ent);
            return;
        }

        var message = Loc.GetString(ent.Comp.DestroyingPopup, ("name", Name(ent)));
        Popup.PopupEntity(message, ent);
        _trigger.StartTimer(ent.Owner, user: null);

        // prevent a shitter borg running into people
        RemComp<InputMoverComponent>(ent);
    }

    private bool CheckEmagged(EntityUid uid, string name)
    {
        if (HasComp<EmaggedComponent>(uid))
        {
            Popup.PopupEntity(Loc.GetString($"borg-transponder-emagged-{name}-popup"), uid, uid, PopupType.LargeCaution);
            return true;
        }

        return false;
    }
}
