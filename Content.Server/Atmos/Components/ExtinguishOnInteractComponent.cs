using Content.Server.Atmos.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.Atmos.Components;
/// <summary>
/// Allows you to extinguish an object by interacting with it
/// </summary>
[RegisterComponent, Access(typeof(FlammableSystem))]
public sealed partial class ExtinguishOnInteractComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ExtinguishAttemptSound = new SoundPathSpecifier("/Audio/Items/candle_blowing.ogg");

    /// <summary>
    /// Extinguishing chance
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Probability = 0.9f;

    /// <summary>
    /// Number of fire stacks to be changed on successful interaction.
    /// </summary>
    // With positive values, the interaction will conversely fan the fire,
    // which is useful for any blacksmithing mechs
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float StackDelta = -5.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public LocId ExtinguishFailed = "candle-extinguish-failed";
}
