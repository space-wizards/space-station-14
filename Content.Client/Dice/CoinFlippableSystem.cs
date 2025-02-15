using Content.Shared.CoinFlippable;
using Robust.Client.GameObjects;

namespace Content.Client.CoinFlippable;

public sealed class CoinFlippableSystem : SharedCoinFlippableSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoinFlippableComponent, AfterAutoHandleStateEvent>(OnDiceAfterHandleState);
    }
    private void OnDiceAfterHandleState(Entity<CoinFlippableComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        // Currently each coin belongs to the same RSI, like dice.
        var state = sprite.LayerGetState(0).Name;
        if (state == null)
            return;

        // This is important to the rsi file names (See CoinFlippableComponent)
        // x_heads.png, x_tails.png, (and x_side.png if used) must all exist in the .rsi
        string currentVal = "error";
        switch (entity.Comp.CurrentValue) // Convert coinFlippable.CurrentValue to its proper string representation for .rsi
        {
            case 0:
                currentVal = "heads";
                break;
            case 1:
                currentVal = "tails";
                break;
            case 2:
                currentVal = "side";
                break;
        }

        var prefix = state.Substring(0, state.LastIndexOf('_'));
        sprite.LayerSetState(0, $"{prefix}_{currentVal}");
    }
}
