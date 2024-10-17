using Content.Shared.Dice;
//using Content.Shared.Popups;
//using Content.Shared.Verbs;
//using Robust.Server.GameObjects;
//using Robust.Shared.Player;
//using Robust.Shared.Utility;

namespace Content.Server.Dice;

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
