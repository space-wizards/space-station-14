using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceNetwork;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Robotics;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Emag.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

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
            if (comp.NextDisable is { } nextDisable && now >= nextDisable)
                DoDisable((uid, comp, chassis, meta));

            if (now < comp.NextBroadcast)
                continue;

            var charge = 0f;
            if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
                charge = battery.CurrentCharge / battery.MaxCharge;

            var hpPercent = CalcHP(uid);

            // checks if it has a brain and if the brain is not a empty MMI (gives false anyway if the fake disable is true)
            var hasBrain = CheckBrain(chassis.BrainEntity) && !comp.FakeDisabled;
            var canDisable = comp.NextDisable == null && !comp.FakeDisabling;
            var data = new CyborgControlData(
                comp.Sprite,
                comp.Name,
                meta.EntityName,
                charge,
                hpPercent,
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

        if (ent.Comp2.BrainEntity is not { } brain)
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
            Destroy(ent.Owner);
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

    /// <summary>
    /// Makes a borg with <see cref="BorgTransponderComponent"/> explode
    /// </summary>
    /// <param name="ent">the entity of the borg</param>
    public void Destroy(Entity<BorgTransponderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // this is stealthy until someone realises you havent exploded
        if (CheckEmagged(ent, "destroyed"))
        {
            // prevent reappearing on the console a few seconds later
            RemComp<BorgTransponderComponent>(ent);
            return;
        }

        var message = Loc.GetString(ent.Comp.DestroyingPopup, ("name", Name(ent)));
        Popup.PopupEntity(message, ent);
        _trigger.ActivateTimerTrigger(ent.Owner);

        // prevent a shitter borg running into people
        RemComp<InputMoverComponent>(ent);
    }

    private bool CheckEmagged(EntityUid uid, string name)
    {
        if (_emag.CheckFlag(uid, EmagType.Interaction))
        {
            Popup.PopupEntity(Loc.GetString($"borg-transponder-emagged-{name}-popup"), uid, uid, PopupType.LargeCaution);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets <see cref="BorgTransponderComponent.Sprite"/>.
    /// </summary>
    public void SetTransponderSprite(Entity<BorgTransponderComponent> ent, SpriteSpecifier sprite)
    {
        ent.Comp.Sprite = sprite;
    }

    /// <summary>
    /// Sets <see cref="BorgTransponderComponent.Name"/>.
    /// </summary>
    public void SetTransponderName(Entity<BorgTransponderComponent> ent, string name)
    {
        ent.Comp.Name = name;
    }

    /// <summary>
    /// Returns a ratio between 0 and 1, 1 when they have no damage and 0 whenever they are crit (or more damaged)
    /// </summary>
    private float CalcHP(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 1;

        if (!_mobState.IsAlive(uid))
            return 0;

        if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var threshold))
        {
            Log.Error($"Borg({ToPrettyString(uid)}), doesn't have critical threshold.");
            return 1;
        }

        return 1 - ((FixedPoint2)(damageable.TotalDamage / threshold)).Float();
    }

    /// <summary>
    /// Returns true if the borg has a brain
    /// </summary>
    private bool CheckBrain(EntityUid? brainEntity)
    {
        if (brainEntity == null)
            return false;

        // if the brainEntity.Value has the component MMIComponent then it is a MMI,
        // in that case it trys to get the "brain" of the MMI, if it is null the MMI is empty and so it returns false
        if (TryComp<MMIComponent>(brainEntity.Value, out var mmi) && _itemSlotsSystem.GetItemOrNull(brainEntity.Value, mmi.BrainSlotId) == null)
            return false;

        return true;
    }
}
