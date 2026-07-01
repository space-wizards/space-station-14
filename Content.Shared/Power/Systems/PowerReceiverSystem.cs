using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Power.Systems;

public sealed partial class PowerReceiverSystem : EntitySystem
{
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ISharedAdminManager _adminManager = default!;
    [Dependency] private PowerNetHandler _handler = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPowerNetSystem _net = default!;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;
    [Dependency] private EntityQuery<PowerReceiverComponent> _recQuery = default!;

    [Dependency] private EntityQuery<PowerNetworkConnectorComponent> _connectorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerSwitchComponent, GetVerbsEvent<AlternativeVerb>>(AddSwitchPowerVerb);

        SubscribeLocalEvent<PowerReceiverComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<PowerReceiverComponent, ProviderConnectedEvent>(OnProviderConnected);
        SubscribeLocalEvent<PowerReceiverComponent, ProviderDisconnectedEvent>(OnProviderDisconnected);

        SubscribeLocalEvent<PowerProviderComponent, ComponentShutdown>(OnProviderShutdown);
        SubscribeLocalEvent<PowerProviderComponent, ReceiverConnectedEvent>(OnReceiverConnected);
        SubscribeLocalEvent<PowerProviderComponent, ReceiverDisconnectedEvent>(OnReceiverDisconnected);

        SubscribeLocalEvent<PowerReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeLocalEvent<PowerReceiverComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PowerReceiverComponent, ComponentHandleState>(OnHandleState);
    }

    private void AddSwitchPowerVerb(Entity<PowerSwitchComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if(!args.CanAccess || !args.CanInteract)
            return;

        if (!_handsQuery.HasComp(args.User))
            return;

        if (!_recQuery.TryGetComponent(ent, out var receiver))
            return;

        if (!receiver.NeedsPower)
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TogglePower((ent.Owner, receiver), user: user);
            },
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Text = Loc.GetString("power-switch-component-toggle-verb"),
            Priority = -3
        };
        args.Verbs.Add(verb);
    }

    public void SetNeedsPower(Entity<PowerReceiverComponent?> ent, bool value)
    {
        if (!_recQuery.TryGetComponent(ent, out var receiver))
            return;

        if (!receiver.NeedsPower == value)
            return;

        receiver.NeedsPower = value;
        Dirty(ent.Owner, receiver);
    }

    public void SetPowerDisabled(Entity<PowerReceiverComponent?> ent, bool value)
    {
        if (!_recQuery.TryGetComponent(ent, out var receiver))
            return;

        if (!receiver.Enabled == value)
            return;

        receiver.Enabled = !value;
        Dirty(ent.Owner, receiver);
    }

    /// <summary>
    /// Turn this machine on or off.
    /// Returns true if we turned it on, false if we turned it off.
    /// </summary>
    public bool TogglePower(Entity<PowerReceiverComponent?> ent, bool playSwitchSound = true, EntityUid? user = null)
    {
        if (!_recQuery.Resolve(ent.Owner, ref ent.Comp))
            return true;

        // it'll save a lot of confusion if 'always powered' means 'always powered'
        if (!ent.Comp.NeedsPower)
        {
            var powered = _net.IsPoweredCalculate(ent.Comp);

            // Server won't raise it here as it can raise the load event later with NeedsPower?
            // This is mostly here for clientside predictions.
            if (ent.Comp.Powered != powered)
            {
                RaisePower(ent!);
            }

            SetPowerDisabled(ent, false);
            return true;
        }

        SetPowerDisabled(ent, ent.Comp.Enabled);

        if (user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user.Value):player} hit power button on {ToPrettyString(ent)}, it's now {(ent.Comp.Enabled ? "on" : "off")}");

        if (playSwitchSound)
        {
            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg"),
                ent,
                user: user,
                AudioParams.Default.WithVolume(-2f));
        }

        if (_netMan.IsClient && !ent.Comp.Enabled)
        {
            var powered = _net.IsPoweredCalculate(ent.Comp);

            // Server won't raise it here as it can raise the load event later with NeedsPower?
            // This is mostly here for clientside predictions.
            if (ent.Comp.Powered != powered)
            {
                ent.Comp.Powered = powered;
                RaisePower(ent!);
            }
        }

        return ent.Comp.Enabled;
    }

    private void RaisePower(Entity<PowerReceiverComponent> entity)
    {
        var ev = new PowerChangedEvent(entity.Comp.Powered, 0f);
        RaiseLocalEvent(entity.Owner, ref ev);
    }

    /// <summary>
    /// Sets the power load of this power receiver.
    /// </summary>
    public void SetLoad(Entity<PowerReceiverComponent?> entity, float load)
    {
        if (!_recQuery.Resolve(entity.Owner, ref entity.Comp))
            return;

        entity.Comp.DesiredPower = load;
    }

    private string GetExamineText(bool powered)
    {
        return Loc.GetString("power-receiver-component-on-examine-main",
                                ("stateText", Loc.GetString(powered
                                    ? "power-receiver-component-on-examine-powered"
                                    : "power-receiver-component-on-examine-unpowered")));
    }

    private void OnExamined(Entity<PowerReceiverComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(GetExamineText(ent.Comp.Powered));
    }

    private void OnGetVerbs(Entity<PowerReceiverComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_adminManager.HasAdminFlag(args.User, AdminFlags.Admin))
            return;

        // add debug verb to toggle power requirements
        args.Verbs.Add(new()
        {
            Text = Loc.GetString("verb-debug-toggle-need-power"),
            Category = VerbCategory.Debug,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")), // "smite" is a lightning bolt
            Act = () =>
            {
                SetNeedsPower(ent.AsNullable(), !ent.Comp.NeedsPower);
            },
        });
    }

    private void OnProviderShutdown(Entity<PowerProviderComponent> ent, ref ComponentShutdown args)
    {
        foreach (var receiver in ent.Comp.LinkedReceivers)
        {
            if (!_recQuery.TryComp(receiver, out var load)
                || !_connectorQuery.TryComp(receiver, out var connector))
                continue;

            load.LinkedNetwork = default;
            if (connector.Net == null)
                continue;

            _handler.QueueNetworkReconnect(connector.Net);
        }

        ent.Comp.LinkedReceivers.Clear();
    }

    private void OnProviderConnected(Entity<PowerReceiverComponent> receiver, ref ProviderConnectedEvent args)
    {
        receiver.Comp.Provider = args.Provider;
        ProviderChanged(receiver);
    }

    private void OnProviderDisconnected(Entity<PowerReceiverComponent> receiver, ref ProviderDisconnectedEvent args)
    {
        receiver.Comp.Provider = null;
        ProviderChanged(receiver);
    }

    private void OnReceiverConnected(Entity<PowerProviderComponent> provider, ref ReceiverConnectedEvent args)
    {
        if (_recQuery.TryComp(args.Receiver, out var receiver)
            && _connectorQuery.TryComp(args.Receiver, out var connector)
            && connector.Net != null)
        {
            _handler.AddReceiver(connector.Net, (args.Receiver, receiver), provider.AsNullable());
        }
    }

    private void OnReceiverDisconnected(Entity<PowerProviderComponent> provider, ref ReceiverDisconnectedEvent args)
    {
        if (_recQuery.TryComp(args.Receiver, out var receiver)
            && _connectorQuery.TryComp(args.Receiver, out var connector)
            && connector.Net != null)
        {
            _handler.RemoveReceiver(connector.Net, (args.Receiver, receiver), provider.AsNullable());
        }
    }

    private void OnGetState(Entity<PowerReceiverComponent> ent, ref ComponentGetState args)
    {
        args.State = new PowerReceiverComponentState
        {
            Powered = ent.Comp.Powered,
            NeedsPower = ent.Comp.NeedsPower,
            Enabled = ent.Comp.Enabled,
        };
    }

    private void OnHandleState(Entity<PowerReceiverComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not PowerReceiverComponentState state)
            return;

        var powerChanged = ent.Comp.Powered != state.Powered;
        ent.Comp.Powered = state.Powered;
        ent.Comp.NeedsPower = state.NeedsPower;
        ent.Comp.Enabled = state.Enabled;
        // SO client systems can handle it. The main reason for this is we can't guarantee compstate ordering.

        if (powerChanged)
            RaisePower(ent);
    }

    private void ProviderChanged(Entity<PowerReceiverComponent> receiver)
    {
        var comp = receiver.Comp;
        comp.LinkedNetwork = default;
    }

    /// <summary>
    /// If this takes power, it returns whether it has power.
    /// Otherwise, it returns 'true' because if something doesn't take power
    /// it's effectively always powered.
    /// </summary>
    /// <returns>True when entity has no PowerReceiverComponent or is Powered. False when not.</returns>
    public bool IsPowered(Entity<PowerReceiverComponent?> ent)
    {
        return !_recQuery.Resolve(ent.Owner, ref ent.Comp, false) || ent.Comp.Powered;
    }
}
