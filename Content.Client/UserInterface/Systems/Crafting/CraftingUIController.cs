using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Crafting;

[UsedImplicitly]
public sealed class CraftingUIController : UIController, IOnStateChanged<GameplayState>
{
    private ConstructionMenuPresenter? _presenter;
    private MenuButton? _craftingButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.CraftingButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_presenter == null);
        _presenter = new ConstructionMenuPresenter();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_presenter == null)
            return;
        UnloadButton(_presenter);
        _presenter.Dispose();
        _presenter = null;
    }

    internal void UnloadButton(ConstructionMenuPresenter? presenter = null)
    {
        if (_craftingButton == null)
        {
            return;
        }

        if (presenter == null)
        {
            presenter ??= _presenter;
            if (presenter == null)
            {
                return;
            }
        }

        _craftingButton.Pressed = false;
        _craftingButton.OnToggled -= presenter.OnHudCraftingButtonToggled;
    }

    public void LoadButton()
    {
        if (_craftingButton == null || _presenter == null)
        {
            return;
        }

        _craftingButton.OnToggled += _presenter.OnHudCraftingButtonToggled;
    }
}
