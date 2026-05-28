using Robust.Shared.GameStates;

namespace Content.Shared.Traitor.Components;

/// <summary>
/// Marks that this item is a hacking beacon that will hack infrastructure it is planted onto.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HackingBeaconComponent : Component;
