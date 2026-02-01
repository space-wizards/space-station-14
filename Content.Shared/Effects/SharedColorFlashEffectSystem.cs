using Robust.Shared.Player;

namespace Content.Shared.Effects;

public abstract class SharedColorFlashEffectSystem : EntitySystem
{
    public const string HitDamageEffect = "HitDamage";
    public const string HitStaminaEffect = "HitStamina";
    public const string HitInvulnerableEffect = "HitInvulnerable";

    protected abstract void RaiseEffect(string effect, Color color, List<EntityUid> entities, Filter filter);

    public void RaiseEffect(string source, List<EntityUid> entities, Filter filter, Color? color = null)
    {
        // If no color specified, set default based on effect source or White
        color ??= source switch
        {
            HitDamageEffect => Color.Red,
            HitStaminaEffect => Color.Aqua,
            HitInvulnerableEffect => Color.Turquoise,
            _ => Color.White,
        };

        RaiseEffect(source, color.Value, entities, filter);
    }
}
