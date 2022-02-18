namespace Content.Client.Orbit;

[RegisterComponent]
public sealed class OrbitVisualsComponent : Component
{
    /// <summary>
    ///     How long should the orbit animation last in seconds?
    /// </summary>
    [DataField("orbitLength")]
    public float OrbitLength = 5.0f;

    /// <summary>
    ///     How long should the orbit stop animation last in seconds?
    /// </summary>
    [DataField("orbitStopLength")]
    public float OrbitStopLength = 2.0f;
}
