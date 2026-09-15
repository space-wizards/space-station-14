using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddMindRole : EntityEffectBase<AddMindRole>
{
    [DataField(required: true)]
    public EntProtoId Role;
}
