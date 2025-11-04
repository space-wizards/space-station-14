using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.VariationPass.Components;

/// <summary>
/// This is used for misplacing entities in a variation pass.
/// </summary>

[RegisterComponent]
public sealed partial class EntityMisplacementVariationPassComponent : Component
{
    /// <summary>
    ///     Name of the prototype to misplace
    /// </summary>
    [DataField(required: true)]
    public EntProtoId MisplacedEntity;

    /// <summary>
    ///     Optional prototype to add in the moved prototype's place
    /// </summary>
    [DataField]
    public EntProtoId? ReplacementEntity;
}
