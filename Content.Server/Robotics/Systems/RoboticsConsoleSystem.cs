using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Lock;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.Robotics;
using Content.Shared.Robotics.Components;
using Content.Shared.Robotics.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Server.Research.Systems;

/// <summary>
/// Handles UI and state receiving for the robotics control console.
/// <c>BorgTransponderComponent<c/> broadcasts state from the station's borgs to consoles.
/// </summary>
public sealed partial class RoboticsConsoleSystem : SharedRoboticsConsoleSystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    private const string CyborgTimerPrefix = "cyborg:";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoboticsConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<RoboticsConsoleComponent, EntityTimerEvent>(OnCyborgTimer);
        Subs.BuiEvents<RoboticsConsoleComponent>(RoboticsConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<RoboticsConsoleDisableMessage>(OnDisable);
            subs.Event<RoboticsConsoleDestroyMessage>(OnDestroy);
            // TODO: camera stuff
        });
    }

    private void OnCyborgTimer(Entity<RoboticsConsoleComponent> ent, ref EntityTimerEvent args)
    {
        if (!args.Id.Value.StartsWith(CyborgTimerPrefix, StringComparison.Ordinal))
            return;

        var address = args.Id.Value[CyborgTimerPrefix.Length..];
        if (ent.Comp.Cyborgs.Remove(address))
            UpdateUserInterface(ent);
    }

    private void OnPacketReceived(Entity<RoboticsConsoleComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;
        if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;
        if (command != DeviceNetworkConstants.CmdUpdatedState)
            return;

        if (!payload.TryGetValue(RoboticsConsoleConstants.NET_CYBORG_DATA, out CyborgControlData? data))
            return;

        var real = data.Value;
        real.Timeout = _timing.CurTime + ent.Comp.Timeout;
        ent.Comp.Cyborgs[args.SenderAddress] = real;
        _timers.SetTimerAt(ent, GetCyborgTimer(args.SenderAddress), real.Timeout);

        UpdateUserInterface(ent);
    }

    private void OnOpened(Entity<RoboticsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnDisable(Entity<RoboticsConsoleComponent> ent, ref RoboticsConsoleDisableMessage args)
    {
        if (!ent.Comp.AllowBorgControl)
            return;

        if (_lock.IsLocked(ent.Owner))
            return;

        if (!ent.Comp.Cyborgs.TryGetValue(args.Address, out var data))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DISABLE_COMMAND
        };

        _deviceNetwork.QueuePacket(ent, args.Address, payload);
        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Actor):user} disabled borg {data.Name} with address {args.Address}");
    }

    private void OnDestroy(Entity<RoboticsConsoleComponent> ent, ref RoboticsConsoleDestroyMessage args)
    {
        if (!ent.Comp.AllowBorgControl)
            return;

        if (_lock.IsLocked(ent.Owner))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextDestroy)
            return;

        if (!ent.Comp.Cyborgs.Remove(args.Address, out var data))
            return;

        _timers.CancelTimer<RoboticsConsoleComponent>(ent, GetCyborgTimer(args.Address));

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DESTROY_COMMAND
        };

        _deviceNetwork.QueuePacket(ent, args.Address, payload);

        var message = Loc.GetString(ent.Comp.DestroyMessage, ("name", data.Name));
        _radio.SendRadioMessage(ent, message, ent.Comp.RadioChannel, ent);
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Actor):user} destroyed borg {data.Name} with address {args.Address}");

        ent.Comp.NextDestroy = now + ent.Comp.DestroyCooldown;
        Dirty(ent, ent.Comp);
    }

    private void UpdateUserInterface(Entity<RoboticsConsoleComponent> ent)
    {
        var state = new RoboticsConsoleState(ent.Comp.Cyborgs, ent.Comp.AllowBorgControl);
        _ui.SetUiState(ent.Owner, RoboticsConsoleUiKey.Key, state);
    }

    private static EntityTimerId GetCyborgTimer(string address)
    {
        return new EntityTimerId(CyborgTimerPrefix + address);
    }
}
