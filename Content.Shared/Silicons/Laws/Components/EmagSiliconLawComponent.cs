namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity that grants a special "obey" law when emagge.d
/// </summary>
[RegisterComponent]
public sealed class EmagSiliconLawComponent : Component
{
    /// <summary>
    /// The name of the person who emagged this law provider.
    /// </summary>
    [DataField("ownerName")]
    public string? OwnerName;
}
