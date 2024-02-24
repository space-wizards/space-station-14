using Content.Shared.Botany.Systems;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Makes produce emit light.
/// The plant itself is not affected.
/// </summary>
public sealed partial class BioluminescentProduceComponent : Component
{
    /// <summary>
    /// Radius of the light.
    /// </summary>
    [DataField]
    public float Radius = 2f;

    /// <summary>
    /// Color of the light.
    /// </summary>
    [DataField]
    public Color Color = Color.White;
}
