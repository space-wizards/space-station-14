using Content.Shared.Shipyard.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard;

[NetSerializable, Serializable]
public enum ShipyardConsoleUiKey : byte
{
    Shipyard
    // Syndicate
    //Not currently implemented. Could be used in the future to give other factions a variety of shuttle options,
    //like nukies, syndicate, or for evac purchases.
}
