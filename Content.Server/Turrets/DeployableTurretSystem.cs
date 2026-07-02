using Content.Server.Destructible;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat.Ranged;
using Content.Shared.Destructible;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.Repairable;
using Content.Shared.TurretController;
using Content.Shared.Turrets;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Turrets;

public sealed partial class DeployableTurretSystem : SharedDeployableTurretSystem
{
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private BatteryWeaponFireModesSystem _fireModes = default!;
    [Dependency] private TurretTargetSettingsSystem _turretTargetingSettings = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeployableTurretComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<DeployableTurretComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<DeployableTurretComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DeployableTurretComponent, BreakageEventArgs>(OnBroken);
        SubscribeLocalEvent<DeployableTurretComponent, RepairedEvent>(OnRepaired);
        SubscribeLocalEvent<DeployableTurretComponent, BeforeBroadcastAttemptEvent>(OnBeforeBroadcast);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<TurretControllerSetArmamentPayload>(OnSetArmament);
        SubscribePayload<TurretControllerSetAccessPayload>(OnSetAccess);
        SubscribePayload<TurretControllerRequestPayload>(OnRequest);
    }

    private void OnAmmoShot(Entity<DeployableTurretComponent> ent, ref AmmoShotEvent args)
    {
        UpdateAmmoStatus(ent);
    }

    private void OnChargeChanged(Entity<DeployableTurretComponent> ent, ref ChargeChangedEvent args)
    {
        UpdateAmmoStatus(ent);
    }

    private void OnPowerChanged(Entity<DeployableTurretComponent> ent, ref PowerChangedEvent args)
    {
        UpdateAmmoStatus(ent);
    }

    private void OnBroken(Entity<DeployableTurretComponent> ent, ref BreakageEventArgs args)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, DeployableTurretVisuals.Broken, true, appearance);

        SetState(ent, false);
    }

    private void OnRepaired(Entity<DeployableTurretComponent> ent, ref RepairedEvent args)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, DeployableTurretVisuals.Broken, false, appearance);
    }

    private void OnSetArmament(Entity<DeployableTurretComponent> ent, ref TurretControllerSetArmamentPayload payload, ref DeviceNetworkPacketData args)
    {
        if (TryComp<BatteryWeaponFireModesComponent>(ent, out var batteryWeaponFireModes))
            _fireModes.TrySetFireMode((ent.Owner, batteryWeaponFireModes), payload.ArmamentState);

        TrySetState(ent, payload.ArmamentState >= 0);
    }

    private void OnSetAccess(Entity<DeployableTurretComponent> ent, ref TurretControllerSetAccessPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!TryComp<TurretTargetSettingsComponent>(ent, out var targetSettings))
            return;

        _turretTargetingSettings.SyncAccessLevelExemptions((ent, targetSettings), payload.AccessExemptions);
    }

    private void OnRequest(Entity<DeployableTurretComponent> ent, ref TurretControllerRequestPayload payload, ref DeviceNetworkPacketData args)
    {
        SendStateUpdateToDeviceNetwork(ent);
    }

    private void OnBeforeBroadcast(Entity<DeployableTurretComponent> ent, ref BeforeBroadcastAttemptEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNetwork))
            return;

        var recipientDeviceNetworks = new HashSet<Device>();

        // Only broadcast to connected devices
        foreach (var recipient in deviceNetwork.DeviceLists)
        {
            if (!TryComp<DeviceNetworkComponent>(recipient, out var recipientDeviceNetwork))
                continue;

            recipientDeviceNetworks.Add(new Device((recipient, recipientDeviceNetwork)));
        }

        if (recipientDeviceNetworks.Count > 0)
            args.ModifiedRecipients = recipientDeviceNetworks;
    }

    private void SendStateUpdateToDeviceNetwork(Entity<DeployableTurretComponent> ent)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var device))
            return;

        var payload = new TurretStatePayload
        {
            State = GetTurretState(ent),
        };
        _deviceNetwork.QueuePacket((ent.Owner, device), null, payload);
    }

    protected override void SetState(Entity<DeployableTurretComponent> ent, bool enabled, EntityUid? user = null)
    {
        if (ent.Comp.Enabled == enabled)
            return;

        base.SetState(ent, enabled, user);
        DirtyField(ent, ent.Comp, nameof(DeployableTurretComponent.Enabled));

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

    private void UpdateAmmoStatus(Entity<DeployableTurretComponent> ent)
    {
        if (!HasAmmo(ent))
            SetState(ent, false);
    }

    private DeployableTurretState GetTurretState(Entity<DeployableTurretComponent> ent, DestructibleComponent? destructable = null, HTNComponent? htn = null)
    {
        Resolve(ent, ref destructable, ref htn);

        if (destructable?.IsBroken == true)
            return DeployableTurretState.Broken;

        if (htn == null || !HasAmmo(ent))
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

        var query = EntityQueryEnumerator<DeployableTurretComponent, DestructibleComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var deployableTurret, out var destructible, out var htn))
        {
            // Check if the turret state has changed since the last update,
            // and if it has, inform the device network
            var ent = new Entity<DeployableTurretComponent>(uid, deployableTurret);
            var newState = GetTurretState(ent, destructible, htn);

            if (newState != deployableTurret.CurrentState)
            {
                deployableTurret.CurrentState = newState;
                DirtyField(uid, deployableTurret, nameof(DeployableTurretComponent.CurrentState));

                SendStateUpdateToDeviceNetwork(ent);

                if (TryComp<AppearanceComponent>(ent, out var appearance))
                    _appearance.SetData(ent, DeployableTurretVisuals.Turret, newState, appearance);
            }
        }
    }
}
