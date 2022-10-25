using Content.Client.Construction.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Crafting;

[UsedImplicitly]
public sealed class CraftingUIController : UIController, IOnStateChanged<GameplayState>
{
    private ConstructionMenuPresenter? _presenter;
    private MenuButton? CraftingButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.CraftingButton;

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
        if (CraftingButton == null)
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

        CraftingButton.Pressed = false;
        CraftingButton.OnToggled -= presenter.OnHudCraftingButtonToggled;
    }

    public void LoadButton()
    {
        if (CraftingButton == null)
        {
            return;
        }

        CraftingButton.OnToggled += ButtonToggled;
    }

    private void ButtonToggled(BaseButton.ButtonToggledEventArgs obj)
    {
        _presenter?.OnHudCraftingButtonToggled(obj);
    }
}
