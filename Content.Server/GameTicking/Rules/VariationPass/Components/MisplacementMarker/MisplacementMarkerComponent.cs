using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components.MisplacementMarker;

/// <summary>
/// This component is used to query entities to be misplaced during the variation pass.
/// </summary>
[RegisterComponent]
public sealed partial class MisplacementMarkerComponent : Component
{
    /// <summary>
    /// Chance that this entity is misplaced
    /// </summary>
    [DataField]
    public float MisplacementChance = 0.04f;

    /// <summary>
    /// Optional prototype to add in the moved prototype's place
    /// </summary>
    [DataField]
    public EntProtoId? ReplacementEntity;
}
