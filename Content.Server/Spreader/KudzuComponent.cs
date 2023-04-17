namespace Content.Server.Spreader;

/// <summary>
/// Handles entities that spread out when they reach the relevant growth level.
/// </summary>
[RegisterComponent]
public sealed class KudzuComponent : Component
{
    /// <summary>
    /// Chance to spread whenever an edge spread is possible.
    /// </summary>
    [DataField("spreadChance")]
    public float SpreadChance = 1f;
}
