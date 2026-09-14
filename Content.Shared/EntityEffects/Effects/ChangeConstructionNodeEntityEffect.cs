namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChangeConstructionNode : EntityEffectBase<ChangeConstructionNode>
{
    /// <summary>
    /// The construction node to change to.
    /// </summary>
    [DataField(required: true)]
    public string Node { get; set; } = string.Empty;
}
