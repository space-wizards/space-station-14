using Content.Server.Audio;
using Content.Server.GatewayStation.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.GatewayStation;
using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.GatewayStation.Systems;

public sealed class StationGatewaySystem : SharedStationGatewaySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSys = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationGatewayConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<StationGatewayConsoleComponent, StationGatewayGateClickMessage>(OnUIGateClicked);

        SubscribeLocalEvent<StationGatewayComponent, LinkedEntityChangedEvent>(OnLinkedChanged);
        SubscribeLocalEvent<StationGatewayComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StationGatewayComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<GatewayChipComponent, MapInitEvent>(OnChipInit);
        SubscribeLocalEvent<GatewayChipComponent, ExaminedEvent>(OnChipExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationGatewayConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.NextUpdateTime > _timing.CurTime)
                continue;

            console.NextUpdateTime += console.UpdateFrequency;

            UpdateUserInterface((uid, console));
        }
    }

    private void OnChipExamined(Entity<GatewayChipComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ConnectedGate is not null)
            args.PushMarkup(Loc.GetString("gateway-console-chip-examine-recorded", ("gate", ent.Comp.ConnectedName)));
    }

    private void OnInteractUsing(Entity<StationGatewayComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<GatewayChipComponent>(args.Used, out var chip))
            return;

        if (chip.ConnectedGate is not null)
        {
            _popup.PopupEntity(Loc.GetString("gateway-console-chip-already-recorded"), ent, args.User);
            return;
        }

        chip.ConnectedGate = ent;
        chip.ConnectedName = ent.Comp.GateName;
        _popup.PopupEntity(Loc.GetString("gateway-console-chip-record"), ent, args.User);
        _audio.PlayPvs(chip.RecordSound, args.Used);

        args.Handled = true;
    }

    private void OnUIGateClicked(Entity<StationGatewayConsoleComponent> ent, ref StationGatewayGateClickMessage args)
    {
        ConsoleInteract(ent, ref args);
        UpdateUserInterface(ent);
    }

    private void ConsoleInteract(Entity<StationGatewayConsoleComponent> ent, ref StationGatewayGateClickMessage args)
    {
        var gate = GetEntity(args.Gateway);

        if (gate is null)
            return;

        if (!_power.IsPowered(gate.Value))
            return;

        if (!TryComp<StationGatewayComponent>(gate.Value, out var gateComp))
            return;

        if (_link.GetLink(gate.Value, out var linkedGate)) //If the pressed gateway is linked to another - cut this connection.
        {
            _link.TryUnlink(gate.Value, linkedGate.Value);
            gateComp.LastLink = null;
        }
        else //If the pressed gateway is not connected to anything...
        {
            if (ent.Comp.SelectedGate is null) //And the console doesn't have Gateway selected - select it.
            {
                ent.Comp.SelectedGate = gate;
            }
            else // And we have a selected gateway - tie them together.
            {
                if (ent.Comp.SelectedGate != gate.Value && _power.IsPowered(ent.Comp.SelectedGate.Value))
                {
                    if (_link.TryLink(gate.Value, ent.Comp.SelectedGate.Value))
                        gateComp.LastLink = ent.Comp.SelectedGate.Value;

                    _appearanceSys.SetData(gate.Value, GatewayPortalVisual.Color, ent.Comp.GatewayColor);
                    _appearanceSys.SetData(ent.Comp.SelectedGate.Value, GatewayPortalVisual.Color, ent.Comp.GatewayColor);
                }
                ent.Comp.SelectedGate = null;
            }
        }
    }

    private void OnLinkedChanged(Entity<StationGatewayComponent> ent, ref LinkedEntityChangedEvent args)
    {
        if (args.NewLinks.Count > 0)
        {
            _ambient.SetAmbience(ent, true);
            _audio.PlayPvs(ent.Comp.LinkSound, ent);
        }
        else
        {
            _ambient.SetAmbience(ent, false);
            _audio.PlayPvs(ent.Comp.UnlinkSound, ent);
        }
    }

    private void OnPowerChanged(Entity<StationGatewayComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            if (_link.GetLink(ent, out var secondLink))
                _link.TryUnlink(ent, secondLink.Value);
        }
        else
        {
            // We look for a portal from our “memory” and see if it's connected to anything. If not, we connect to it ourselves.
            if (ent.Comp.LastLink is null)
                return;

            if (_link.GetLink(ent.Comp.LastLink.Value, out _))
            {
                ent.Comp.LastLink = null;
                return;
            }

            _link.TryLink(ent, ent.Comp.LastLink.Value);
        }
    }

    private void OnUIOpened(Entity<StationGatewayConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<StationGatewayConsoleComponent> ent)
    {
        if (!_uiSystem.IsUiOpen(ent.Owner, StationGatewayUIKey.Key))
            return;

        // The grid must have a NavMapComponent to visualize the map in the UI
        var xform = Transform(ent);

        if (xform.GridUid != null)
            EnsureComp<NavMapComponent>(xform.GridUid.Value);

        //Send data
        List<StationGatewayStatus> gatewaysData = new();
        var cachedGates = new List<EntityUid>(); //Prevent UI gate dublication

        if (_container.TryGetContainer(ent, ent.Comp.ChipStorageName, out var container))
        {
            foreach (var chip in container.ContainedEntities)
            {
                if (!TryComp<GatewayChipComponent>(chip, out var chipComp))
                    continue;

                if (!EntityManager.EntityExists(chipComp.ConnectedGate))
                    continue;

                if (!TryComp<StationGatewayComponent>(chipComp.ConnectedGate, out var gateway))
                    continue;

                if (cachedGates.Contains(chipComp.ConnectedGate.Value))
                    continue;

                var powered = _power.IsPowered(chipComp.ConnectedGate.Value);

                _link.GetLink(chipComp.ConnectedGate.Value, out var linkedGateway);
                EntityCoordinates? linkCoord = null;
                if (linkedGateway is not null)
                    linkCoord = Transform(linkedGateway.Value).Coordinates;

                cachedGates.Add(chipComp.ConnectedGate.Value);

                gatewaysData.Add(
                    new(GetNetEntity(chipComp.ConnectedGate.Value),
                        GetNetCoordinates(Transform(chipComp.ConnectedGate.Value).Coordinates),
                        GetNetEntity(chipComp.ConnectedGate.Value),
                        GetNetCoordinates(linkCoord),
                        gateway.GateName,
                        powered));
            }
        }
        _uiSystem.SetUiState(ent.Owner, StationGatewayUIKey.Key, new StationGatewayState(gatewaysData, GetNetEntity(ent.Comp.SelectedGate)));
    }

    private void OnChipInit(Entity<GatewayChipComponent> chip, ref MapInitEvent args)
    {
        if (chip.Comp.AutoLinkKey is null)
            return;

        var query = EntityQueryEnumerator<StationGatewayComponent>();
        var successLink = false;
        while (query.MoveNext(out var uid, out var gate))
        {
            if (gate.AutoLinkKey is null || gate.AutoLinkKey != chip.Comp.AutoLinkKey)
                continue;

            chip.Comp.ConnectedGate = uid;
            chip.Comp.ConnectedName = gate.GateName;
            successLink = true;
            break;
        }
        if (!successLink && chip.Comp.DeleteOnFailedLink)
            QueueDel(chip);
    }
}
