using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Medical.Windows;
using Content.Shared.Medical.Wounds.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Medical;

[UsedImplicitly]
public sealed class MedicalUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,
    IOnSystemChanged<WoundSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [UISystemDependency] private readonly WoundSystem _woundSystem = default!;

    private MenuButton? MedicalButton =>
        UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.MedicalButton;

    private MedicalWindow? _window;

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<MedicalWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }

    public void OnSystemLoaded(WoundSystem system)
    {
    }

    public void OnSystemUnloaded(WoundSystem system)
    {
    }

    public void UnloadButton()
    {
        if (MedicalButton == null)
        {
            return;
        }

        MedicalButton.Pressed = false;
        MedicalButton.OnPressed -= MedicalButtonOnPressed;
    }

    public void LoadButton()
    {
        if (MedicalButton == null)
        {
            return;
        }

        MedicalButton.OnPressed += MedicalButtonOnPressed;

        if (_window == null)
        {
            return;
        }

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;
    }

    private void DeactivateButton() => MedicalButton!.Pressed = false;
    private void ActivateButton() => MedicalButton!.Pressed = true;

    private void MedicalButtonOnPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        if (MedicalButton != null)
        {
            MedicalButton.Pressed = !_window.IsOpen;
        }

        if (_window.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            //_characterInfo.RequestCharacterInfo();
            _window.Open();
        }
    }
}
