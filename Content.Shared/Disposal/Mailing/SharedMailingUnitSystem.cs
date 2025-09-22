using Content.Shared.Configurable;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Player;

namespace Content.Shared.Disposal.Mailing;

public abstract class SharedMailingUnitSystem : EntitySystem
{
    [Dependency] private readonly SharedDeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UserInterfaceSystem = default!;

    private const string MailTag = "mail";

    private const string TagConfigurationKey = "tag";

    private const string NetTag = "tag";
    private const string NetSrc = "src";
    private const string NetTarget = "target";
    private const string NetCmdSent = "mail_sent";
    private const string NetCmdRequest = "get_mailer_tag";
    private const string NetCmdResponse = "mailer_tag";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MailingUnitComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MailingUnitComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<MailingUnitComponent, BeforeDisposalFlushEvent>(OnBeforeFlush);
        SubscribeLocalEvent<MailingUnitComponent, ConfigurationUpdatedEvent>(OnConfigurationUpdated);
        SubscribeLocalEvent<MailingUnitComponent, ActivateInWorldEvent>(HandleActivate, before: new[] { typeof(SharedDisposalUnitSystem) });
        SubscribeLocalEvent<MailingUnitComponent, TargetSelectedMessage>(OnTargetSelected);
    }

    private void OnComponentInit(Entity<MailingUnitComponent> ent, ref ComponentInit args)
    {
        UpdateTargetList(ent);
    }

    private void OnPacketReceived(Entity<MailingUnitComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || !_power.IsPowered(ent.Owner))
            return;

        switch (command)
        {
            case NetCmdRequest:
                SendTagRequestResponse(ent, args, ent.Comp.Tag);
                break;
            case NetCmdResponse when args.Data.TryGetValue(NetTag, out string? tag):
                //Add the received tag request response to the list of targets
                ent.Comp.TargetList.Add(tag);
                Dirty(ent);
                break;
        }
    }

    /// <summary>
    /// Sends the given tag as a response to a <see cref="NetCmdRequest"/> if it's not null
    /// </summary>
    private void SendTagRequestResponse(EntityUid uid, DeviceNetworkPacketEvent args, string? tag)
    {
        if (tag == null)
            return;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = NetCmdResponse,
            [NetTag] = tag
        };

        _deviceNetworkSystem.QueuePacket(uid, args.Address, payload, args.Frequency);
    }

    /// <summary>
    /// Prevents the unit from flushing if no target is selected
    /// </summary>
    private void OnBeforeFlush(Entity<MailingUnitComponent> ent, ref BeforeDisposalFlushEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Target))
        {
            args.Cancel();
            return;
        }

        Dirty(ent);
        args.Tags.Add(MailTag);
        args.Tags.Add(ent.Comp.Target);

        BroadcastSentMessage(ent);
    }

    /// <summary>
    /// Broadcast that a mail was sent including the src and target tags
    /// </summary>
    private void BroadcastSentMessage(Entity<MailingUnitComponent> ent, DeviceNetworkComponent? device = null)
    {
        if (string.IsNullOrEmpty(ent.Comp.Tag) || string.IsNullOrEmpty(ent.Comp.Target) || !Resolve(ent, ref device))
            return;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = NetCmdSent,
            [NetSrc] = ent.Comp.Tag,
            [NetTarget] = ent.Comp.Target
        };

        _deviceNetworkSystem.QueuePacket(ent, null, payload, null, null, device);
    }

    /// <summary>
    /// Clears the units target list and broadcasts a <see cref="NetCmdRequest"/>.
    /// The target list will then get populated with <see cref="NetCmdResponse"/> responses from all active mailing units on the same grid
    /// </summary>
    private void UpdateTargetList(Entity<MailingUnitComponent> ent, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(ent, ref device, false))
            return;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = NetCmdRequest
        };

        ent.Comp.TargetList.Clear();
        _deviceNetworkSystem.QueuePacket(ent, null, payload, null, null, device);
    }

    /// <summary>
    /// Gets called when the units tag got updated
    /// </summary>
    private void OnConfigurationUpdated(Entity<MailingUnitComponent> ent, ref ConfigurationUpdatedEvent args)
    {
        var configuration = args.Configuration.Config;
        if (!configuration.ContainsKey(TagConfigurationKey) || configuration[TagConfigurationKey] == string.Empty)
        {
            ent.Comp.Tag = null;
            return;
        }

        ent.Comp.Tag = configuration[TagConfigurationKey];
        Dirty(ent);
    }

    private void HandleActivate(Entity<MailingUnitComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
        {
            return;
        }

        args.Handled = true;
        UpdateTargetList(ent);
        UserInterfaceSystem.OpenUi(ent.Owner, MailingUnitUiKey.Key, actor.PlayerSession);
    }

    private void OnTargetSelected(Entity<MailingUnitComponent> ent, ref TargetSelectedMessage args)
    {
        ent.Comp.Target = args.Target;
        Dirty(ent);
    }
}
