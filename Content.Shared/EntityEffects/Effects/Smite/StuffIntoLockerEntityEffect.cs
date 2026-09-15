using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class StuffIntoLocker : EntityEffectBase<StuffIntoLocker>
{
    [DataField(required: true)]
    public EntProtoId Prototype;
}
