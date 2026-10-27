namespace Content.Shared.EntityEffects.Effects.Construction;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddConstructionContainer : EntityEffectBase<AddConstructionContainer>
{
    [DataField(required: true)]
    public required string Container;
}
