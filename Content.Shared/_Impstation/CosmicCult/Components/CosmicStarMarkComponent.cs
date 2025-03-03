using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Impstation.CosmicCult.Components;

/// <summary>
/// Component for revealing cosmic cultists to the crew.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicStarMarkComponent : Component
{
    public ResPath RsiPath = new("/Textures/_Impstation/CosmicCult/Effects/cultrevealed.rsi");

    public readonly string States = "vfx";
}

[Serializable, NetSerializable]
public enum CosmicRevealedKey
{
    Key
}
