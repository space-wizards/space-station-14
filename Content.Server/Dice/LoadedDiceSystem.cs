using Content.Shared.Dice;

namespace Content.Server.Dice;

/// <summary>
///     Handles overriding the roll of a loaded die.
/// </summary>
public sealed class LoadedDiceSystem : SharedLoadedDiceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadedDiceComponent, DiceRollEvent>(OnDiceRoll);
    }

    private void OnDiceRoll(EntityUid uid, LoadedDiceComponent component, ref DiceRollEvent roll)
    {
        if (component.SelectedSide == null)
            return;

        roll.Roll = component.SelectedSide.Value;
    }
}
