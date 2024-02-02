using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Research.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Research;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Research.Systems;

/// <summary>
/// Handles UI and state receiving for the robotics control console.
/// <c>BorgTransponderComponent<c/> broadcasts state from the station's borgs to consoles.
/// </summary>
public sealed class RoboticsConsoleSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    // almost never timing out more than 1 per tick so initialize with that capacity
    private List<string> _removing = new(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoboticsConsoleComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<RoboticsConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        Subs.BuiEvents<RoboticsConsoleComponent>(RoboticsConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<RoboticsConsoleDisableMessage>(OnDisable);
            subs.Event<RoboticsConsoleDestroyMessage>(OnDestroy);
            // TODO: camera stuff
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        foreach (var comp in EntityQuery<RoboticsConsoleComponent>())
        {
            // remove cyborgs that havent pinged in a while
            _removing.Clear();
            foreach (var (address, timeout) in comp.Timeouts)
            {
                if (now >= timeout)
                    _removing.Add(address);
            }

            // needed to prevent modifying while iterating it
            foreach (var address in _removing)
            {
                comp.Cyborgs.Remove(address);
                comp.Timeouts.Remove(address);
            }
        }
    }

    private void OnUnpaused(Entity<RoboticsConsoleComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextDestroy += args.PausedTime;
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

        var address = args.SenderAddress;
        ent.Comp.Cyborgs[address] = data.Value;
        ent.Comp.Timeouts[address] = _timing.CurTime + ent.Comp.Timeout;

        UpdateUserInterface(ent);
    }

    private void OnOpened(Entity<RoboticsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnDisable(Entity<RoboticsConsoleComponent> ent, ref RoboticsConsoleDisableMessage args)
    {
        if (args.Session.AttachedEntity is not {} user || !_access.IsAllowed(user, ent))
            return;

        if (!ent.Comp.Cyborgs.TryGetValue(args.Address, out var data))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DISABLE_COMMAND
        };

        _deviceNetwork.QueuePacket(ent, args.Address, payload);
        _adminLogger.Add(LogType.RoboticsConsole, LogImpact.High, $"{ToPrettyString(user):user} disabled borg {data.Name} with address {args.Address}");
    }

    private void OnDestroy(Entity<RoboticsConsoleComponent> ent, ref RoboticsConsoleDestroyMessage args)
    {
        if (args.Session.AttachedEntity is not {} user || !_access.IsAllowed(user, ent))
            return;

        var now = _timing.CurTime;
        if (now < ent.Comp.NextDestroy)
            return;

        if (!RemoveCyborg(ent, args.Address, out var data))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = RoboticsConsoleConstants.NET_DESTROY_COMMAND
        };

        _deviceNetwork.QueuePacket(ent, args.Address, payload);

        var message = Loc.GetString(ent.Comp.DestroyMessage, ("name", data.Value.Name));
        _radio.SendRadioMessage(ent, message, ent.Comp.RadioChannel, ent);
        _adminLogger.Add(LogType.RoboticsConsole, LogImpact.Extreme, $"{ToPrettyString(user):user} destroyed borg {data.Value.Name} with address {args.Address}");

        ent.Comp.NextDestroy = now + ent.Comp.DestroyCooldown;
    }

    private bool RemoveCyborg(Entity<RoboticsConsoleComponent> ent, string address, [NotNullWhen(true)] out CyborgControlData? data)
    {
        data = null;
        if (!ent.Comp.Cyborgs.Remove(address, out var real))
            return false;

        data = real;

        return ent.Comp.Timeouts.Remove(address);
    }

    private void UpdateUserInterface(Entity<RoboticsConsoleComponent> ent)
    {
        var state = new RoboticsConsoleState(ent.Comp.Cyborgs);
        _ui.TrySetUiState(ent, RoboticsConsoleUiKey.Key, state);
    }
}
