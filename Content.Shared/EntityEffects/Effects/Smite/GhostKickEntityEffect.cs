namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class GhostKickEffect : EntityEffectBase<GhostKickEffect>
{
    [DataField(required: true)]
    public LocId Reason;
}
