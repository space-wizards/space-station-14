using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.Abilities.Borgs;

/// <summary>
/// Marks this entity as being candy with a random flavor and color.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomizedCandyComponent : Component
{
}

[Serializable, NetSerializable]
public enum RandomizedCandyVisuals : byte
{
    Color
}
