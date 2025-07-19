using Content.Shared.Effects;
using Robust.Shared.Player;

namespace Content.Server.Effects;

public sealed class ColorFlashEffectSystem : SharedColorFlashEffectSystem
{
    protected override void RaiseEffect(string source, Color color, List<EntityUid> entities, Filter filter)
    {
        RaiseNetworkEvent(new ColorFlashEffectEvent(source, color, GetNetEntityList(entities)), filter);
    }
}
