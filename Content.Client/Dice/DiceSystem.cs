using Content.Shared.Dice;
using Robust.Client.GameObjects;

namespace Content.Client.Dice;

public sealed class DiceSystem : SharedDiceSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiceComponent, AfterAutoHandleStateEvent>(OnDiceAfterHandleState);
    }

    private void OnDiceAfterHandleState(Entity<DiceComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        // TODO maybe just move each die to its own RSI?
        var state = sprite.LayerGetState(0).Name;
        if (state == null)
            return;

        var prefix = state.Substring(0, state.IndexOf('_'));
        sprite.LayerSetState(0, $"{prefix}_{entity.Comp.CurrentValue}");
    }
}
