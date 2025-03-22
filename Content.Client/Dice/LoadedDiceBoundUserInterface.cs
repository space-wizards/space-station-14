using System.Linq;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Shared.Dice;

namespace Content.Client.Dice;

/// <summary>
///     Bound UI to set the roll of a loaded die.
/// </summary>
[UsedImplicitly]
public sealed class LoadedDiceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private LoadedDiceMenu? _menu;

    public LoadedDiceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<LoadedDiceMenu>();
        _menu.OnSideSelected += OnSideSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not LoadedDiceBoundUserInterfaceState st)
            return;

        // Generate the Enumerable of concrete side values from the Offset, Sides, and Multiplier
        // This makes it easier for the menu to display the values without having to compute them all the time
        var possibleSides = Enumerable.Range(1, st.Sides).Select(i => new DiceSide(i, ((i - st.Offset) * st.Multiplier).ToString()));
        _menu?.UpdateState(possibleSides, st.SelectedSide);
    }

    private void OnSideSelected(int? selectedSide)
    {
        SendMessage(new LoadedDiceSideSelectedMessage(selectedSide));
    }
}
