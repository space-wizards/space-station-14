using Content.Shared.CoinFlippable;
using Robust.Client.GameObjects;

namespace Content.Client.CoinFlippable;

public sealed class CoinFlippableSystem : SharedCoinFlippableSystem
{
    protected override void UpdateVisuals(EntityUid uid, CoinFlippableComponent? coinFlippable = null)
    {
        if (!Resolve(uid, ref coinFlippable) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        var state = sprite.LayerGetState(0).Name;
        if (state == null)
            return;

        string currentVal = "error";
        switch (coinFlippable.CurrentValue) // Convert coinFlippable.CurrentValue to its proper string representation
        {
            case 0:
                currentVal ="heads";
                break;
            case 1:
                currentVal ="tails";
                break;
            case 2:
                currentVal ="side";
                break;
        }

        var prefix = state.Substring(0, state.LastIndexOf('_'));
        sprite.LayerSetState(0, $"{prefix}_{currentVal}");
    }
}
