using Content.Shared.Dice;
using Robust.Client.GameObjects;

namespace Content.Client.Dice;

public sealed class DiceSystem : SharedDiceSystem
{
    protected override void UpdateVisuals(EntityUid uid, DiceComponent? die = null)
    {
        if (!Resolve(uid, ref die) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        // d100 shows a 10 for 1->10, a 20 for 11->20, etc.
        var displayedSide = ((die.CurrentSide - 1) / die.Step + 1) * die.Step;
        sprite.LayerSetState(0, $"d{die.Sides}_{displayedSide}");
    }
}
