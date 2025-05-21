using Content.Shared.Dice;
using Robust.Client.GameObjects;

namespace Content.Client.Dice;

public sealed class DiceSystem : SharedDiceSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

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
        var state = _sprite.LayerGetRsiState((entity.Owner, sprite), 0).Name;
        if (state == null)
            return;

        var prefix = state.Substring(0, state.IndexOf('_'));
        _sprite.LayerSetRsiState((entity.Owner, sprite), 0, $"{prefix}_{entity.Comp.CurrentValue}");
    }
}
