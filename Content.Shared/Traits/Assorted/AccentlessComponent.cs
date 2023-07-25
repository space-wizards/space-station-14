using Robust.Shared.Prototypes;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for the accentless trait
/// </summary>
[RegisterComponent]
public sealed class AccentlessComponent : Component
{
    /// <summary>
    ///     The accents removed by the accentless trait.
    /// </summary>
    [DataField("components")]
    public ComponentRegistry RemovedAccents = new();
}
