using Content.Server.Shuttles.Components;

namespace Content.Server.Shuttle;

public sealed class ShuttleDestinationCoordinatesSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entManager = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public EntityUid? GetDestinationEntityUid(string destination)
    {
        //For other destinations, this needs to be reworked, as it defaults to the first CentComm option available.
        if (destination == "Central Command")
        {
            var query = _entManager.AllEntityQueryEnumerator<StationCentcommComponent>();
            while (query.MoveNext(out var comp))
            {
                return comp.Entity;
            }
        }
        return null;
    }
}


