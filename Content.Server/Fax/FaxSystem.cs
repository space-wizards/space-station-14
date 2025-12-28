using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Fax;
using Content.Shared.Fax.Components;
using Content.Shared.Fax.Systems;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.NameModifier.Components;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Fax;

public sealed class FaxSystem : SharedFaxSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly FaxecuteSystem _faxecute = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Hooks
        SubscribeLocalEvent<FaxMachineComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<FaxMachineComponent, DeviceNetworkPacketEvent>(OnPacketReceived);

        SubscribeLocalEvent<FaxMachineComponent, FaxRefreshMessage>(OnRefreshButtonPressed);
    }

    private void OnMapInit(Entity<FaxMachineComponent> ent, ref MapInitEvent args)
    {
        // Load all faxes on map in cache each other to prevent taking same name by user created fax
        Refresh(ent);
    }

    private void OnPacketReceived(Entity<FaxMachineComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (string.IsNullOrEmpty(args.SenderAddress))
            return;

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case FaxConstants.FaxPingCommand:
                    var isForSyndie = _emag.CheckFlag(ent.Owner, EmagType.Interaction) &&
                                      args.Data.ContainsKey(FaxConstants.FaxSyndicateData);
                    if (!isForSyndie && !ent.Comp.ResponsePings)
                        return;

                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, FaxConstants.FaxPongCommand },
                        { FaxConstants.FaxNameData, ent.Comp.FaxName }
                    };
                    _deviceNetworkSystem.QueuePacket(ent.Owner, args.SenderAddress, payload);

                    break;
                case FaxConstants.FaxPongCommand:
                    if (!args.Data.TryGetValue(FaxConstants.FaxNameData, out string? faxName))
                        return;

                    ent.Comp.KnownFaxes[args.SenderAddress] = faxName;

                    UpdateUserInterface(ent);
                    Dirty(ent);

                    break;
                case FaxConstants.FaxPrintCommand:
                    if (!args.Data.TryGetValue(FaxConstants.FaxPaperNameData, out string? name) ||
                        !args.Data.TryGetValue(FaxConstants.FaxPaperContentData, out string? content))
                        return;

                    args.Data.TryGetValue(FaxConstants.FaxPaperLabelData, out string? label);
                    args.Data.TryGetValue(FaxConstants.FaxPaperStampStateData, out string? stampState);
                    args.Data.TryGetValue(FaxConstants.FaxPaperStampedByData, out List<StampDisplayInfo>? stampedBy);
                    args.Data.TryGetValue(FaxConstants.FaxPaperPrototypeData, out string? prototypeId);
                    args.Data.TryGetValue(FaxConstants.FaxPaperLockedData, out bool? locked);

                    var printout = new FaxPrintout(content, name, label, prototypeId, stampState, stampedBy, locked ?? false);

                    Receive((ent.Owner, ent.Comp), printout, args.SenderAddress);
                    break;
            }
        }
    }

    private void OnRefreshButtonPressed(Entity<FaxMachineComponent> ent, ref FaxRefreshMessage args)
    {
        Refresh(ent);
    }

    /// <summary>
    ///     Clears current known fax info and make network scan ping
    ///     Adds special data to  payload if it was emagged to identify itself as a Syndicate
    /// </summary>
    public void Refresh(Entity<FaxMachineComponent> ent)
    {
        ent.Comp.DestinationFaxAddress = null;
        ent.Comp.KnownFaxes.Clear();

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxConstants.FaxPingCommand }
        };

        if (_emag.CheckFlag(ent.Owner, EmagType.Interaction))
            payload.Add(FaxConstants.FaxSyndicateData, true);

        _deviceNetworkSystem.QueuePacket(ent.Owner, null, payload);
        Dirty(ent);
    }

    /// <summary>
    ///     Sends message to addressee if paper is set and a known fax is selected
    ///     A timeout is set after sending, which is shared by the copy button.
    /// </summary>
    protected override void Send(Entity<FaxMachineComponent> ent, ref FaxSendMessage args)
    {
        var sendEntity = ent.Comp.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (ent.Comp.DestinationFaxAddress == null)
            return;

        if (!ent.Comp.KnownFaxes.TryGetValue(ent.Comp.DestinationFaxAddress, out var faxName))
            return;

        if (!TryComp(sendEntity, out MetaDataComponent? metadata) ||
           !TryComp<PaperComponent>(sendEntity, out var paper))
            return;

        base.Send(ent, ref args);


        TryComp<NameModifierComponent>(sendEntity, out var nameMod);

        TryComp<LabelComponent>(sendEntity, out var labelComponent);

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxConstants.FaxPrintCommand },
            { FaxConstants.FaxPaperNameData, nameMod?.BaseName ?? metadata.EntityName },
            { FaxConstants.FaxPaperLabelData, labelComponent?.CurrentLabel },
            { FaxConstants.FaxPaperContentData, paper.Content },
            { FaxConstants.FaxPaperLockedData, paper.EditingDisabled },
        };

        if (metadata.EntityPrototype != null)
        {
            // TODO: Ideally, we could just make a copy of the whole entity when it's
            // faxed, in order to preserve visuals, etc.. This functionality isn't
            // available yet, so we'll pass along the originating prototypeId and fall
            // back to component.PrintPaperId in SpawnPaperFromQueue if we can't find one here.
            payload[FaxConstants.FaxPaperPrototypeData] = metadata.EntityPrototype.ID;
        }

        if (paper.StampState != null)
        {
            payload[FaxConstants.FaxPaperStampStateData] = paper.StampState;
            payload[FaxConstants.FaxPaperStampedByData] = paper.StampedBy;
        }

        _deviceNetworkSystem.QueuePacket(ent.Owner, ent.Comp.DestinationFaxAddress, payload);

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):actor} " +
            $"sent fax from \"{ent.Comp.FaxName}\" {ToPrettyString(ent.Owner):tool} " +
            $"to \"{faxName}\" ({ent.Comp.DestinationFaxAddress}) " +
            $"of {ToPrettyString(sendEntity):subject}: {paper.Content}");
    }

    protected override void OnPrintedEvent(Entity<FaxMachineComponent> ent, ref PrintedEvent args)
    {
        base.OnPrintedEvent(ent, ref args);

        if (TryComp<PaperComponent>(ent.Owner, out var paper))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"\"{ent.Comp.FaxName}\" {ToPrettyString(ent.Owner):tool} printed {ToPrettyString(args.Printed):subject}: {paper.Content}");
        }
        else
        {
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"\"{ent.Comp.FaxName}\" {ToPrettyString(ent.Owner):tool} printed without PaperComponent {ToPrettyString(args.Printed):subject}");
        }
    }

    protected override void NotifyAdmins(string faxName)
    {
        _chat.SendAdminAnnouncement(Loc.GetString("fax-machine-chat-notify", ("fax", faxName)));
        _audioSystem.PlayGlobal("/Audio/Machines/high_tech_confirm.ogg", Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false, AudioParams.Default.WithVolume(-8f));
    }
}

public struct OnPaperPrintEvent
{
    public Entity<PaperComponent> paper;
}
