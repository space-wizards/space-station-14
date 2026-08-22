namespace Content.Server.Cargo.Components;

/// <summary>
/// This is used for marking containers as
/// containing goods for fulfilling bounties.
/// </summary>
[RegisterComponent, AutoGenerateEntityRelations]
public sealed partial class CargoBountyLabelComponent : Component
{
    /// <summary>
    /// The ID for the bounty this label corresponds to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Id = string.Empty;

    /// <summary>
    /// Used to prevent recursion in calculating the price.
    /// </summary>
    public bool Calculating;

    /// <summary>
    /// The Station System to check and remove bounties from
    /// </summary>
    [DataField, AutoRelationField]
    public EntityRelation AssociatedStationId;
}
