using Content.Client.Gameplay;
using Content.Client.Humanoid;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Medical.Windows;
using Content.Shared.Body.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Medical;

[UsedImplicitly]
public sealed class MedicalUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>,
    IOnSystemChanged<WoundSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly PlayerManager _playerManager = default!;
    [UISystemDependency] private readonly WoundSystem _woundSystem = default!;

    private EntityUid? _medicalFocus;

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

    //returns true if the target has medical options
    private bool HasMedicalOptions(EntityUid target, out BodyComponent? bodyComp, out WoundableComponent? woundableComp)
    {
        woundableComp = null;
        return _entities.TryGetComponent(target, out bodyComp) ||
               _entities.TryGetComponent(target, out woundableComp);
    }

    public string GetMedicalFocusName(EntityUid target)
    {
        return Identity.Name(target, _entities);
    }

    public void SetMedicalFocus(EntityUid target)
    {
        if (!HasMedicalOptions(target, out var bodyComp, out var woundableComp))
            return; //if the target does not have a body or woundable components do not allow it to be set as a medical focus
        UpdateMedicalFocus(target);
    }

    private bool TryGetDummySprites(EntityUid focusedEntity, out SpriteComponent? frontDummy,
        out SpriteComponent? backDummy)
    {
        frontDummy = null;
        backDummy = null;

        if (_entities.TryGetComponent(focusedEntity, out HumanoidComponent? characterComp))
        {
            return true;
        }

        return false;
    }

    private void UpdateMedicalFocus(EntityUid newFocus)
    {
        _medicalFocus = newFocus;
        //TODO: refetch wounds for the focused entity
        if (_window == null)
            return;
        TryGetDummySprites(newFocus, out var frontDummy, out var backDummy);
        _window.StatusDisplay.UpdateUI(frontDummy, backDummy, GetMedicalFocusName(newFocus));
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
