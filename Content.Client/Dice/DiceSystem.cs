using Content.Shared.Dice;
using Robust.Client.GameObjects;

namespace Content.Client.Dice;

public sealed class DiceSystem : SharedDiceSystem
{
    protected override void UpdateVisuals(Entity<DiceComponent> entity)
    {
        if (!TryComp(entity, out SpriteComponent? sprite))
            return;

        // TODO maybe just move each diue to its own RSI?
        var state = sprite.LayerGetState(0).Name;
        if (state == null)
            return;

        var prefix = state.Substring(0, state.IndexOf('_'));
        sprite.LayerSetState(0, $"{prefix}_{entity.Comp.CurrentValue}");
    }
}
