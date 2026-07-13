using Robust.Shared.Player;

namespace Content.Shared.Effects;

public abstract class SharedColorFlashEffectSystem : EntitySystem
{
    /// <summary>
    /// Causes the effect on entities.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="entities"></param>
    /// <param name="filter"></param>
    public abstract void RaiseEffect(Color color, List<EntityUid> entities, Filter filter);
}
