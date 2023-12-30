using Content.Server.Shuttles.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Server.Station.Systems;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Shuttle.Components;

/// <summary>
/// Enables a shuttle/pod to travel to a destination with an item inserted
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleDestinationCoordinatesComponent : Component
{

    //This component should be able to return a destination EntityUid based on the whitelist datafield.
    //Right now that functionality is not implemented, defaulting to the Central Command map as only one item (CentCom Coords Disk) uses this component.

    [DataField("whitelist")]
    public string Destination = "Central Command";

    [Dependency] private readonly EntityManager _entManager = default!;

    public EntityUid? GetDestinationEntityUid()
    {
        //For other destinations, this needs to be reworked, as it defaults to the first CentComm option available.
        var query = _entManager.AllEntityQueryEnumerator<StationCentcommComponent>();
        while (query.MoveNext(out var comp))
        {
            return comp.Entity;
        }
        return null;
    }
}
