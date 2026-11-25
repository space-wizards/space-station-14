using Robust.Shared.Prototypes;

namespace Content.Shared.Gibbing.Components;

/// <summary>
/// Gibs an entity on round end.
/// </summary>
[RegisterComponent]
public sealed partial class GibOnRoundEndComponent : Component
{
    /// <summary>
    /// If the entity has all these objectives fulfilled they won't be gibbed.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> PreventGibbingObjectives = new();

    /// <summary>
    /// Entity to spawn when gibbed. Can be used for effects.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnProto;
}
