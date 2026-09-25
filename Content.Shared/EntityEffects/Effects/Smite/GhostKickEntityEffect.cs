namespace Content.Shared.EntityEffects.Effects.Smite;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class GhostKickEffect : EntityEffectBase<GhostKickEffect>
{
    /// <summary>
    /// Localization key of the disconnect reason shown to the kicked player.
    /// </summary>
    [DataField(required: true)]
    public LocId Reason;
}
