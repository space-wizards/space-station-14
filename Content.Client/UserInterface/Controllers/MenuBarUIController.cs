using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.HUD.Widgets;
using Robust.Client.State;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Controllers;

public sealed class MenuBarUIController : UIController
{
    [Dependency] private readonly IHudManager _hud = default!;

    public override void OnStateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            GameplayStateEntered();
        }
    }

    private void GameplayStateEntered()
    {
        var bar = _hud.GetUIWidget<MenuBar>();

        bar.InfoButton.OnPressed += InfoButtonPressed;
    }

    private void InfoButtonPressed(ButtonEventArgs args)
    {
        throw new NotImplementedException();
    }
}
