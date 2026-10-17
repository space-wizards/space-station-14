namespace Content.Shared.EntityEffects.Effects.Construction;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddContainer : EntityEffectBase<AddContainer>
{
    [DataField(required: true)]
    public required string Container;
}
