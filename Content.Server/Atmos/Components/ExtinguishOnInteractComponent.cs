using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Server.Atmos.Components;
/// <summary>
/// Allows you to extinguish an object by interacting with it
/// </summary>
[RegisterComponent]
public sealed partial class ExtinguishOnInteractComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ExtinguishSound = new SoundPathSpecifier("/Audio/Items/candle_blowing.ogg");

    /// <summary>
    /// Extinguishing chance
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Probability = 0.9f;

    /// <summary>
    /// Number of fire stacks to be removed on successful interaction
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StackDelta = -5.0f;
}
