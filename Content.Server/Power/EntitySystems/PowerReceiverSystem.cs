using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Managers;
using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

public sealed partial class PowerReceiverSystem : SharedPowerReceiverSystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private PowerNetHandler _handler = default!;

    [Dependency] private EntityQuery<PowerNetworkConnectorComponent> _connectorQuery = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerReceiverComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<PowerReceiverComponent, ProviderConnectedEvent>(OnProviderConnected);
        SubscribeLocalEvent<PowerReceiverComponent, ProviderDisconnectedEvent>(OnProviderDisconnected);

        SubscribeLocalEvent<PowerProviderComponent, ComponentShutdown>(OnProviderShutdown);
        SubscribeLocalEvent<PowerProviderComponent, ReceiverConnectedEvent>(OnReceiverConnected);
        SubscribeLocalEvent<PowerProviderComponent, ReceiverDisconnectedEvent>(OnReceiverDisconnected);

        SubscribeLocalEvent<PowerReceiverComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeLocalEvent<PowerReceiverComponent, ComponentGetState>(OnGetState);
    }

    private void OnExamined(Entity<PowerReceiverComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(GetExamineText(ent.Comp.Powered));
    }

    private void OnGetVerbs(EntityUid uid, PowerReceiverComponent component, GetVerbsEvent<Verb> args)
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
                SetNeedsPower(uid, !component.NeedsPower, component);
            }
        });
    }

    private void OnProviderShutdown(EntityUid uid, PowerProviderComponent component, ComponentShutdown args)
    {
        foreach (var receiver in component.LinkedReceivers)
        {
            if (!RecQuery.TryComp(receiver, out var load)
                || !_connectorQuery.TryComp(receiver, out var connector))
                continue;

            load.LinkedNetwork = default;
            if (connector.Net == null)
                continue;

            _handler.QueueNetworkReconnect(connector.Net);
        }

        component.LinkedReceivers.Clear();
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
        if (RecQuery.TryComp(args.Receiver, out var receiver)
            && _connectorQuery.TryComp(args.Receiver, out var connector)
            && connector.Net != null)
        {
            _handler.AddReceiver(connector.Net, (args.Receiver, receiver), provider.AsNullable());
        }
    }

    private void OnReceiverDisconnected(Entity<PowerProviderComponent> provider, ref ReceiverDisconnectedEvent args)
    {
        if (RecQuery.TryComp(args.Receiver, out var receiver)
            && _connectorQuery.TryComp(args.Receiver, out var connector)
            && connector.Net != null)
        {
            _handler.RemoveReceiver(connector.Net, (args.Receiver, receiver), provider.AsNullable());
        }
    }

    private void OnGetState(EntityUid uid, PowerReceiverComponent component, ref ComponentGetState args)
    {
        args.State = new PowerReceiverComponentState
        {
            Powered = component.Powered,
            NeedsPower = component.NeedsPower,
            Enabled = component.Enabled,
        };
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
    public bool IsPowered(EntityUid uid, PowerReceiverComponent? receiver = null)
    {
        return !RecQuery.Resolve(uid, ref receiver, false) || receiver.Powered;
    }

    public override bool ResolveApc(EntityUid entity, [NotNullWhen(true)] ref PowerReceiverComponent? component)
    {
        if (component != null)
            return true;

        if (!TryComp(entity, out PowerReceiverComponent? receiver))
            return false;

        component = receiver;
        return true;
    }
}
