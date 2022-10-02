using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fax;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Fax;

// TODO: Emag change frequency to Syndicate
// TODO: Sending cooldown
// TODO: Correct visualizer for paper-insert/sending/receiving/idle
// TODO: Support not only text send? Not only paper? But how serialize?
// TODO: Add separate paper container for new messages? Add ink? Add paper jamming?
// TODO: Messages receive queue and send history?
// TODO: Fax wires hacking?
// TODO: Allow rename fax with multitool
// TODO: Add receive messages queue for printing

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<FaxMachineComponent>())
        {
            if (comp.PrintingTimeRemaining > 0)
                comp.PrintingTimeRemaining -= frameTime;
            if (comp.InsertingTimeRemaining > 0)
                comp.InsertingTimeRemaining -= frameTime;

            if (comp.PrintingTimeRemaining <= 0)
            {
                SpawnPaperFromBuffer(comp.Owner, comp);
            }

            if (comp.InsertingTimeRemaining <= 0)
            {
                _itemSlotsSystem.SetLock(comp.Owner, comp.PaperSlot, false);
            }
            
            UpdateAppearance(comp.Owner, comp);
        }
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

        var isPaperInserted = component.PaperSlot.Item.HasValue;
        if (isPaperInserted)
        {
            component.InsertingTimeRemaining = component.InsertionTime;
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
        }
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
        foreach (var (faxAddress, faxName) in component.KnownFaxes)
        {
            args.Verbs.Add(new()
            {
                Text = faxName,
                Message = Loc.GetString("fax-machine-verb-destination-desc"),
                Category = VerbCategory.FaxDestination,
                Act = () => SetDestination(uid, faxAddress),
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
                    if (!component.IsVisibleInNetwork)
                        return;

                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, FaxMachineConstants.FaxPongCommand },
                        { FaxMachineConstants.FaxNameData, component.FaxName }
                    };
                    _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, payload);

                    break;
                case FaxMachineConstants.FaxPongCommand:
                    if (!args.Data.TryGetValue(FaxMachineConstants.FaxNameData, out string? faxName))
                        return;

                    component.KnownFaxes[args.SenderAddress] = faxName;

                    break;
                case FaxMachineConstants.FaxPrintCommand:
                    if (!args.Data.TryGetValue(FaxMachineConstants.FaxContentData, out string? content))
                        return;

                    Print(uid, content, args.SenderAddress);

                    break;
            }
        }
    }

    private void UpdateAppearance(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.InsertingTimeRemaining > 0)
            _appearance.SetData(uid, FaxMachineVisuals.BaseState, FaxMachineVisualState.Inserting);
        else if (component.PrintingTimeRemaining > 0)
            _appearance.SetData(uid, FaxMachineVisuals.BaseState, FaxMachineVisualState.Printing);
        else
            _appearance.SetData(uid, FaxMachineVisuals.BaseState, FaxMachineVisualState.Normal);
    }

    public void SetDestination(EntityUid uid, string destAddress, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxAddress = destAddress;

        var faxName = Loc.GetString("fax-machine-popup-source-unknown");
        if (component.KnownFaxes.ContainsKey(destAddress)) // If admin manually set address unknown for fax
            faxName = component.KnownFaxes[destAddress];

        var msg = Loc.GetString("fax-machine-popup-destination", ("destination", faxName));
        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));
    }

    public void RefreshFaxes(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxAddress = null;
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

        if (component.DestinationFaxAddress == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-destination-not-selected"), uid, Filter.Pvs(uid));
            return;
        }
        
        if (!component.KnownFaxes.TryGetValue(component.DestinationFaxAddress, out var faxName))
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
        _deviceNetworkSystem.QueuePacket(uid, component.DestinationFaxAddress, payload);

        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-send"), uid, Filter.Pvs(uid));
    }

    public void Print(EntityUid uid, string content, string? fromAddress, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var faxName = Loc.GetString("fax-machine-popup-source-unknown");
        if (fromAddress != null && component.KnownFaxes.ContainsKey(fromAddress)) // If message received from unknown for fax address
            faxName = component.KnownFaxes[fromAddress];

        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-received", ("from", faxName)), uid, Filter.Pvs(uid));
        _appearance.SetData(uid, FaxMachineVisuals.BaseState, FaxMachineVisualState.Printing);

        component.TextBuffer = content;

        component.PrintingTimeRemaining = component.PrintingTime;
        UpdateAppearance(uid, component);
    }

    private void SpawnPaperFromBuffer(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        
        if (component.TextBuffer == null)
            return;

        var printed = EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        if (!TryComp<PaperComponent>(printed, out var paper))
            return;

        _paperSystem.SetContent(printed, component.TextBuffer);
        component.TextBuffer = null;
    }
}
