namespace Content.Server.Icarus;

[RegisterComponent]
public sealed class IcarusBeamComponent : Component
{
    /// <summary>
    ///     Beam moving speed.
    /// </summary>
    [DataField("speed")]
    public float Speed = 1f;

    /// <summary>
    ///     The beam will be automatically cleaned up after this time.
    /// </summary>
    [DataField("lifetime")]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(240);

    /// <summary>
    ///     With this set to true, beam will automatically set the tiles under them to space.
    /// </summary>
    [DataField("destroyTiles")]
    public bool DestroyTiles = true;

    [DataField("destroyRadius")]
    public float DestroyRadius = 2f;

    [DataField("flameRadius")]
    public float FlameRadius = 4f;

    [DataField("accumulator")]
    public float Accumulator = 0f;
}
