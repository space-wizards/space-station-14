using Robust.Shared.Player;

namespace Content.Shared.Effects;

public abstract class SharedColorFlashEffectSystem : EntitySystem
{
    public abstract void RaiseEffect(Color color, List<EntityUid> entities, Filter filter);

    public void RaisePredictedEffect(Color color, List<EntityUid> entities, Filter filter, EntityUid? recipient)
    {
        if (recipient != null)
            filter.RemovePlayerByAttachedEntity(recipient.Value);

        RaiseEffect(color, entities, filter);
    }
}
