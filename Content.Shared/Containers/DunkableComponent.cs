using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers;

/// <summary>
///     Modifies interactions related to dunking / throwing!
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DunkableComponent : Component
{
    /// <summary>
    ///     Modifies how likely the entity is to get thrown into containers. Higher numbers mean the item is more likely to be
    ///     inserted. Should never be negative.
    /// </summary>
    [DataField(required: true)]
    public float Dunkability;
}
