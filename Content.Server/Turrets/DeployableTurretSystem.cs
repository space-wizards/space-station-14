using Content.Server.NPC.HTN;
using Content.Server.Power.Components;
using Content.Server.Repairable;
using Content.Shared.Destructible;
using Content.Shared.Turrets;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.DeviceNetwork;
using Content.Shared.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Ranged;
using Content.Shared.Weapons.Ranged.Systems;

// The following will be uncommented by the turret control panel PR
//using Content.Server.TurretController;  
//using Content.Shared.Weapons.Ranged.Components;
//using Content.Shared.Access;
//using Robust.Shared.Prototypes;

namespace Content.Server.Turrets;

public sealed partial class DeployableTurretSystem : SharedDeployableTurretSystem
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly BatteryWeaponFireModesSystem _fireModes = default!;
    [Dependency] private readonly TurretTargetSettingsSystem _turretTargetingSettings = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeployableTurretComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<DeployableTurretComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<DeployableTurretComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DeployableTurretComponent, BreakageEventArgs>(OnBroken);
        SubscribeLocalEvent<DeployableTurretComponent, RepairedEvent>(OnRepaired);
        //SubscribeLocalEvent<DeployableTurretComponent, DeviceNetworkPacketEvent>(OnPacketReceived); // Will be uncommented by the turret control panel PR
        SubscribeLocalEvent<DeployableTurretComponent, BeforeBroadcastAttemptEvent>(OnBeforeBroadcast);
    }

    private void OnAmmoShot(Entity<DeployableTurretComponent> ent, ref AmmoShotEvent args)
    {
        if (!HasAmmo(ent))
            SetState(ent, false);
    }

    private void OnChargeChanged(Entity<DeployableTurretComponent> ent, ref ChargeChangedEvent args)
    {
        if (!HasAmmo(ent))
            SetState(ent, false);
    }

    private void OnPowerChanged(Entity<DeployableTurretComponent> ent, ref PowerChangedEvent args)
    {
        ent.Comp.Powered = args.Powered;
        Dirty(ent);

        if (!HasAmmo(ent))
            SetState(ent, false);
    }

    private void OnBroken(Entity<DeployableTurretComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, DeployableTurretVisuals.Broken, true, appearance);

        SetState(ent, false);
    }

    private void OnRepaired(Entity<DeployableTurretComponent> ent, ref RepairedEvent args)
    {
        ent.Comp.Broken = false;
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, DeployableTurretVisuals.Broken, false, appearance);
    }

    /* The following Will be uncommented by the turret control panel PR
    private void OnPacketReceived(Entity<DeployableTurretComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        // Received a command to change armament state
        if (command == DeployableTurretControllerSystem.CmdSetArmamemtState &&
            args.Data.TryGetValue(command, out int? armamentState))
        {
            if (TryComp<BatteryWeaponFireModesComponent>(ent, out var batteryWeaponFireModes))
                _fireModes.TrySetFireMode(ent, batteryWeaponFireModes, armamentState.Value);

            TrySetState(ent, armamentState.Value >= 0);
            return;
        }

        // Received a command to change access exemptions
        if (command == DeployableTurretControllerSystem.CmdSetAccessExemptions &&
            args.Data.TryGetValue(command, out HashSet<ProtoId<AccessLevelPrototype>>? accessExemptions) &&
            TryComp<TurretTargetSettingsComponent>(ent, out var turretTargetSettings))
        {
            _turretTargetingSettings.SyncAccessLevelExemptions((ent, turretTargetSettings), accessExemptions);
            return;
        }

        // Received a command to update the device network
        if (command == DeviceNetworkConstants.CmdUpdatedState)
        {
            SendStateUpdateToDeviceNetwork(ent);
            return;
        }
    }*/

    private void OnBeforeBroadcast(Entity<DeployableTurretComponent> ent, ref BeforeBroadcastAttemptEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork))
            return;

        var recipientDeviceNetworks = new HashSet<DeviceNetworkComponent>();

        // Only broadcast to connected devices
        foreach (var recipient in deviceNetwork.DeviceLists)
        {
            if (!TryComp<DeviceNetworkComponent>(recipient, out var recipientDeviceNetwork))
                continue;

            recipientDeviceNetworks.Add(recipientDeviceNetwork);
        }

        if (recipientDeviceNetworks.Count > 0)
            args.ModifiedRecipients = recipientDeviceNetworks;
    }

    private void SendStateUpdateToDeviceNetwork(Entity<DeployableTurretComponent> ent)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var device))
            return;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [DeviceNetworkConstants.CmdUpdatedState] = GetTurretState(ent)
        };

        _deviceNetwork.QueuePacket(ent, null, payload, device: device);
    }

    protected override void SetState(Entity<DeployableTurretComponent> ent, bool enabled, EntityUid? user = null)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        base.SetState(ent, enabled, user);
        Dirty(ent);

        // Determine how much time is remaining in the current animation and the one next in queue
        var animTimeRemaining = MathF.Max((float)(ent.Comp.AnimationCompletionTime - _timing.CurTime).TotalSeconds, 0f);
        var animTimeNext = ent.Comp.Enabled ? ent.Comp.DeploymentLength : ent.Comp.RetractionLength;

        // End/restart any tasks the NPC was doing
        // Delay the resumption of any tasks based on the total animation length (plus a buffer)
        var planCooldown = animTimeRemaining + animTimeNext + 0.5f;

        if (TryComp<HTNComponent>(ent, out var htn))
            _htn.SetHTNEnabled((ent, htn), ent.Comp.Enabled, planCooldown);

        // Play audio
        _audio.PlayPvs(ent.Comp.Enabled ? ent.Comp.DeploymentSound : ent.Comp.RetractionSound, ent, new AudioParams { Volume = -10f });
    }

    private DeployableTurretState GetTurretState(Entity<DeployableTurretComponent> ent)
    {
        if (!TryComp<HTNComponent>(ent, out var htn) ||
            ent.Comp.Broken || !HasAmmo(ent))
            return DeployableTurretState.Disabled;

        if (htn.Plan?.CurrentTask.Operator is GunOperator)
            return DeployableTurretState.Firing;

        if (ent.Comp.AnimationCompletionTime > _timing.CurTime)
            return ent.Comp.Enabled ? DeployableTurretState.Deploying : DeployableTurretState.Retracting;

        return ent.Comp.Enabled ? DeployableTurretState.Deployed : DeployableTurretState.Retracted;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DeployableTurretComponent>();
        while (query.MoveNext(out var uid, out var deployableTurret))
        {
            // Check if the turret state has changed since the last update,
            // and if it has, inform the device network
            var ent = new Entity<DeployableTurretComponent>(uid, deployableTurret);
            var newState = GetTurretState(ent);

            if (newState != deployableTurret.CurrentState)
            {
                deployableTurret.CurrentState = newState;
                SendStateUpdateToDeviceNetwork(ent);

                if (TryComp<AppearanceComponent>(ent, out var appearance))
                    _appearance.SetData(ent, DeployableTurretVisuals.Turret, newState, appearance);
            }
        }
    }
}
