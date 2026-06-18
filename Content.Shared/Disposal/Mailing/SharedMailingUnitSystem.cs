using Content.Shared.Configurable;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Player;

namespace Content.Shared.Disposal.Mailing;

public abstract partial class SharedMailingUnitSystem : EntitySystem
{
    [Dependency] private SharedDeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    private const string MailTag = "mail";
    private const string TagConfigurationKey = "tag";

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
        if (!_power.IsPowered(uid))
            return;

        switch (args.Data)
        {
            case MailRequestTagPayload:
                SendTagRequestResponse(uid, args, component.Tag);
                break;
            case MailTagPayload payload:
                //Add the received tag request response to the list of targets
                component.TargetList.Add(payload.Tag);
                Dirty(uid, component);
                break;
        }
    }

    /// <summary>
    /// Sends the given tag as a response to a <see cref="MailRequestTagPayload"/> if it's not null
    /// </summary>
    private void SendTagRequestResponse(EntityUid uid, DeviceNetworkPacketEvent args, string? tag)
    {
        if (tag == null)
            return;

        var payload = new MailTagPayload
        {
            Tag = tag,
        };

        _deviceNetwork.QueuePacket(uid, args.Address, payload, args.Frequency);
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

        var payload = new MailSendPayload
        {
            Tag = component.Tag,
            Target = component.Target
        };

        _deviceNetworkSystem.QueuePacket((uid, device), null, payload);
    }

    /// <summary>
    /// Clears the units target list and broadcasts a <see cref="MailRequestTagPayload"/>.
    /// The target list will then get populated with <see cref="MailTagPayload"/> responses from all active mailing units on the same grid
    /// </summary>
    private void UpdateTargetList(Entity<MailingUnitComponent> ent, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(ent, ref device, false))
            return;

        var payload = new MailRequestTagPayload();
        component.TargetList.Clear();
        _deviceNetworkSystem.QueuePacket((uid, device), null, payload);
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
            return;

        args.Handled = true;
        UpdateTargetList(ent);

        _userInterface.OpenUi(ent.Owner, MailingUnitUiKey.Key, actor.PlayerSession);
    }

    private void OnTargetSelected(Entity<MailingUnitComponent> ent, ref TargetSelectedMessage args)
    {
        ent.Comp.Target = args.Target;
        Dirty(ent);

        if (_userInterface.TryGetOpenUi(ent.Owner, MailingUnitUiKey.Key, out var bui))
        {
            bui.Update<MailingUnitBoundUserInterfaceState>();
        }
    }
}
