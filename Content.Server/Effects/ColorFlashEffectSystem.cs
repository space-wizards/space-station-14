using Content.Shared.Effects;
using Robust.Shared.Player;

namespace Content.Server.Effects;

public sealed class ColorFlashEffectSystem : SharedColorFlashEffectSystem
{
    public override void RaiseEffect(Color color, List<EntityUid> entities, Filter filter)
    {
        var netEntities = ToNetEntityList(entities);

        RaiseNetworkEvent(new ColorFlashEffectEvent(color, netEntities), filter);
    }
}
