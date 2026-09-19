namespace Content.Server.CosmicCult.Components;

/// <summary>
///     Associates an entity with a specific cosmic cult gamerule
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCultAssociatedRuleComponent : Component
{
    /// <summary>
    ///     The gamerule that this entity is associated with
    /// </summary>
    [DataField]
    public EntityUid CultGamerule;
}
