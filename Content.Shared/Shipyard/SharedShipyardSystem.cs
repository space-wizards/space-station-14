using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard;

public abstract class SharedShipyardSystem : EntitySystem { }

[NetSerializable, Serializable]
public enum ShipyardConsoleUiKey : byte
{
    Shipyard
    // Syndicate
    //Not currently implemented. Could be used in the future to give other factions a variety of shuttle options,
    //like nukies, syndicate, or for evac purchases.
}
