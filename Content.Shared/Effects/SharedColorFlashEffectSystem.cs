using Robust.Shared.Player;

namespace Content.Shared.Effects;

public enum EffectSource : byte
{
    Other,
    HitDamage,
    HitStamina,
    HitDamageInvulnerable,
    HitFluid,
}

public abstract class SharedColorFlashEffectSystem : EntitySystem
{

    protected abstract void RaiseEffect(EffectSource effect, Color color, List<EntityUid> entities, Filter filter);

    public void RaiseEffect(EffectSource source, List<EntityUid> entities, Filter filter, Color? color = null)
    {
        // If no color specified, set default based on effect source or White
        color ??= source switch
        {
            EffectSource.HitDamage => Color.Red,
            EffectSource.HitStamina => Color.Aqua,
            EffectSource.HitDamageInvulnerable => Color.Turquoise,
            _ => Color.White,
        };

        RaiseEffect(source, color.Value, entities, filter);
    }
}
