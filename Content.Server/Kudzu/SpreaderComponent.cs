namespace Content.Server.Kudzu;

/// <summary>
/// Component for rapidly spreading objects, like Kudzu.
/// ONLY USE THIS FOR ANCHORED OBJECTS. An error will be logged if not anchored/static.
/// Currently does not support growing in space.
/// </summary>
[RegisterComponent, Access(typeof(SpreaderSystem))]
public sealed class SpreaderComponent : Component
{
    /// <summary>
    /// Chance for it to grow on any given tick, after the normal growth rate-limit (if it doesn't grow, SpreaderSystem will pick another one.).
    /// </summary>
    [DataField("chance", required: true)]
    public float Chance;

    /// <summary>
    /// Prototype spawned on growth success.
    /// </summary>
    [DataField("growthResult", required: true)]
    public string GrowthResult = default!;

    [DataField("enabled")]
    public bool Enabled = true;
}
