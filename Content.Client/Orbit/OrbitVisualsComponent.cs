using Robust.Shared.Animations;

namespace Content.Client.Orbit;

[RegisterComponent]
public sealed class OrbitVisualsComponent : Component
{
    /// <summary>
    ///     How long should the orbit animation last in seconds, before being randomized?
    /// </summary>
    [DataField("orbitLengthBase")]
    public float OrbitLength = 2.0f;

    /// <summary>
    ///     How far away from the entity should the orbit be, before being randomized?
    /// </summary>
    [DataField("orbitDistanceBase")]
    public float OrbitDistance = 1.0f;

    /// <summary>
    ///     Does this entity rotate clockwise or counterclockwise?
    ///     Always randomized.
    /// </summary>
    public bool Clockwise = false;

    /// <summary>
    ///     How long should the orbit stop animation last in seconds?
    /// </summary>
    [DataField("orbitStopLength")]
    public float OrbitStopLength = 1.0f;

    /// <summary>
    ///     How far along in the orbit, from 0 to 1, is this entity?
    /// </summary>
    [Animatable]
    public float Orbit { get; set; } = 0.0f;

    public bool Orbiting = false;
}
