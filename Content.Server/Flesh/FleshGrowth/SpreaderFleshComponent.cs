namespace Content.Server.Flesh.FleshGrowth;

/// <summary>
/// Component for rapidly spreading objects, like Kudzu.
/// ONLY USE THIS FOR ANCHORED OBJECTS. An error will be logged if not anchored/static.
/// Currently does not support growing in space.
/// </summary>
[RegisterComponent, Access(typeof(SpreaderFleshSystem))]
public sealed class SpreaderFleshComponent : Component
{
    /// <summary>
    /// Chance for it to grow on any given tick, after the normal growth rate-limit (if it doesn't grow, SpreaderSystem will pick another one.).
    /// </summary>
    [DataField("chance", required: true)]
    public float Chance;

    /// <summary>
    /// Maximum number of edges that can grow out every interval.
    /// </summary>
    private const int GrowthsPerInterval = 1;

    /// <summary>
    /// Prototype spawned on growth success.
    /// </summary>
    [DataField("growthResult", required: true)]
    public string GrowthResult = default!;

    /// <summary>
    /// Prototype spawned on growth success.
    /// </summary>
    [DataField("wallResult", required: true)]
    public string WallResult = default!;

    [DataField("enabled")]
    public bool Enabled = true;
}
