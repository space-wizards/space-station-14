using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Fax;

// TODO: Emag change frequency to Syndicate
// TODO: Sending cooldown
// TODO: Display placed paper
// TODO: Support not only text send? Not only paper? But how serialize?
// Faxes with same id not adds as different

public sealed class FaxMachineSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    
    public const string PaperSlotId = "FaxMachine-paper";

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<FaxMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FaxMachineComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<FaxMachineComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<FaxMachineComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<FaxMachineComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<FaxMachineComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnComponentInit(EntityUid uid, FaxMachineComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, FaxMachineSystem.PaperSlotId, component.PaperSlot);
    }

    private void OnComponentRemove(EntityUid uid, FaxMachineComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }
    
    private void OnItemSlotChanged(EntityUid uid, FaxMachineComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)
            return;

        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, FaxMachineComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;
        
        // TODO: Update
    }
    
    private void OnVerb(EntityUid uid, FaxMachineComponent component, GetVerbsEvent<Verb> args)
    {
        // standard interaction checks
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        
        // send verb
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("fax-machine-verb-send"),
            Message = Loc.GetString("fax-machine-verb-send-desc"),
            Act = () => Send(uid),
        });
        
        // refresh network verb
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("fax-machine-verb-refresh"),
            Message = Loc.GetString("fax-machine-verb-refresh-desc"),
            Act = () => RefreshFaxes(uid),
        });

        // destination select verbs
        foreach (var (faxId, _) in component.KnownFaxes)
        {
            args.Verbs.Add(new()
            {
                Text = faxId,
                Message = Loc.GetString("fax-machine-verb-destination-desc"),
                Category = VerbCategory.FaxDestination,
                Act = () => SetDestination(uid, faxId),
            });
        }
    }
    
    private void OnPacketReceived(EntityUid uid, FaxMachineComponent component, DeviceNetworkPacketEvent args)
    {
        if (string.IsNullOrEmpty(args.SenderAddress))
            return;
        
        if (!TryComp(uid, out DeviceNetworkComponent? deviceNet))
            return;

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case FaxMachineConstants.FaxPingCommand:
                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPongCommand },
                        { FaxMachineConstants.FaxIdData, component.FaxId }
                    };
                    _deviceNetworkSystem.QueuePacket(uid, null, payload);

                    break;
                case FaxMachineConstants.FaxPongCommand:
                    if (!args.Data.TryGetValue(FaxMachineConstants.FaxIdData, out string? faxId))
                        return;

                    if (!component.KnownFaxes.ContainsKey(faxId))
                        component.KnownFaxes.Add(faxId, args.SenderAddress);

                    break;
                case FaxMachineConstants.FaxPrintCommand:
                    if (!args.Data.TryGetValue(FaxMachineConstants.FaxContentData, out string? content))
                        return;

                    Print(uid, content);

                    break;
            }
        }
    }

    public void SetDestination(EntityUid uid, string destinationFaxId, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxId = destinationFaxId;

        var msg = Loc.GetString("fax-machine-popup-destination", ("destination", destinationFaxId));
        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));
    }

    public void RefreshFaxes(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxId = null;
        component.KnownFaxes.Clear();
        
        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPingCommand }
        };
        _deviceNetworkSystem.QueuePacket(uid, null, payload);
    }

    public void Send(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var sendEntity = component.PaperSlot.Item;
        if (sendEntity == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-paper-not-inserted"), uid, Filter.Pvs(uid));
            return;
        }

        if (component.DestinationFaxId == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-destination-not-selected"), uid, Filter.Pvs(uid));
            return;
        }

        if (!component.KnownFaxes.TryGetValue(component.DestinationFaxId, out string? destinationAddress))
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-destination-not-found"), uid, Filter.Pvs(uid));
            return;
        }

        if (!TryComp<PaperComponent>(sendEntity, out var paper))
            return;

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPrintCommand },
            { FaxMachineConstants.FaxContentData, paper.Content }
        };
        _deviceNetworkSystem.QueuePacket(uid, destinationAddress, payload);

        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-send"), uid, Filter.Pvs(uid));
    }

    public void Print(EntityUid uid, string content, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        
        var printed = EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        _paperSystem.SetContent(printed, content);
    }
}
