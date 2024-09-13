using Content.Server.Audio;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GatewayStation.Components;
using Content.Shared.Audio;
using Content.Shared.GatewayStation;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.GatewayStation.Systems;

public sealed class StationGatewaySystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationGatewayConsoleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<StationGatewayConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<StationGatewayConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<StationGatewayConsoleComponent, StationGatewayGateClickMessage>(OnUIGateClicked);

        SubscribeLocalEvent<StationGatewayComponent, LinkedEntityChangedEvent>(OnLinkedChanged);
        SubscribeLocalEvent<StationGatewayComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnUIGateClicked(Entity<StationGatewayConsoleComponent> ent, ref StationGatewayGateClickMessage args)
    {
        var gate = GetEntity(args.Gateway);

        if (gate is null)
            return;


        //TODO error sounds

        if (_link.GetLink(gate.Value, out var linkedGate)) //If the pressed gateway is linked to another - cut this connection.
        {
            _link.TryUnlink(gate.Value, linkedGate.Value);
        }
        else //If the pressed gateway is not connected to anything...
        {
            if (ent.Comp.SelectedGate is null) //And the console doesn't have Gateway selected - select it.
            {
                ent.Comp.SelectedGate = gate;
            }
            else // And we have a selected gateway - tie them together.
            {
                if (ent.Comp.SelectedGate != gate.Value)
                    _link.TryLink(gate.Value, ent.Comp.SelectedGate.Value);

                ent.Comp.SelectedGate = null;
            }
        }
        UpdateUserInterface(ent);
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
        throw new NotImplementedException();
    }

    private void OnRemove(Entity<StationGatewayConsoleComponent> ent, ref ComponentRemove args)
    {
        //Clear data
    }

    private void OnPacketReceived(Entity<StationGatewayConsoleComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var payload = args.Data;

        //Update data

        UpdateUserInterface(ent);
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

        var query = EntityQueryEnumerator<StationGatewayComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var gate, out var xformComp))
        {
            if (xform.GridUid != Transform(ent).GridUid)
                return;

            _link.GetLink(uid, out var link);
            EntityCoordinates? linkCoord = null;

            if (link is not null)
                linkCoord = Transform(link.Value).Coordinates;

            gatewaysData.Add(
                new(GetNetEntity(uid),
                    GetNetCoordinates(xformComp.Coordinates),
                    GetNetCoordinates(linkCoord),
                    gate.GateName));
        }
        _uiSystem.SetUiState(ent.Owner, StationGatewayUIKey.Key, new StationGatewayState(gatewaysData, GetNetEntity(ent.Comp.SelectedGate)));
    }
}
