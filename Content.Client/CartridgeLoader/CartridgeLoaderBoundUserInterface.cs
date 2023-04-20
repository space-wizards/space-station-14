using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader;


public abstract class CartridgeLoaderBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager? _entityManager = default!;

    private EntityUid? _activeProgram;
    private UIFragment? _activeCartridgeUI;
    private Control? _activeUiFragment;

    protected CartridgeLoaderBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CartridgeLoaderUiState loaderUiState)
        {
            _activeCartridgeUI?.UpdateState(state);
            return;
        }

        var programs = GetCartridgeComponents(loaderUiState.Programs);
        UpdateAvailablePrograms(programs);

        _activeProgram = loaderUiState.ActiveUI;

        var ui = RetrieveCartridgeUI(loaderUiState.ActiveUI);
        var comp = RetrieveCartridgeComponent(loaderUiState.ActiveUI);
        var control = ui?.GetUIFragmentRoot();

        //Prevent the same UI fragment from getting disposed and attached multiple times
        if (_activeUiFragment?.GetType() == control?.GetType())
            return;

        if (_activeUiFragment is not null)
            DetachCartridgeUI(_activeUiFragment);

        if (control is not null && _activeProgram.HasValue)
        {
            AttachCartridgeUI(control, Loc.GetString(comp?.ProgramName ?? "default-program-name"));
            SendCartridgeUiReadyEvent(_activeProgram.Value);
        }

        _activeCartridgeUI = ui;
        _activeUiFragment?.Dispose();
        _activeUiFragment = control;
    }

    protected void ActivateCartridge(EntityUid cartridgeUid)
    {
        var message = new CartridgeLoaderUiMessage(cartridgeUid, CartridgeUiMessageAction.Activate);
        SendMessage(message);
    }

    protected void DeactivateActiveCartridge()
    {
        if (!_activeProgram.HasValue)
            return;

        var message = new CartridgeLoaderUiMessage(_activeProgram.Value, CartridgeUiMessageAction.Deactivate);
        SendMessage(message);
    }

    protected void InstallCartridge(EntityUid cartridgeUid)
    {
        var message = new CartridgeLoaderUiMessage(cartridgeUid, CartridgeUiMessageAction.Install);
        SendMessage(message);
    }

    protected void UninstallCartridge(EntityUid cartridgeUid)
    {
        var message = new CartridgeLoaderUiMessage(cartridgeUid, CartridgeUiMessageAction.Uninstall);
        SendMessage(message);
    }

    private List<(EntityUid, CartridgeComponent)> GetCartridgeComponents(List<EntityUid> programs)
    {
        var components = new List<(EntityUid, CartridgeComponent)>();

        foreach (var program in programs)
        {
            var component = RetrieveCartridgeComponent(program);
            if (component is not null)
                components.Add((program, component));
        }

        return components;
    }

    /// <summary>
    /// The implementing ui needs to add the passed ui fragment as a child to itself
    /// </summary>
    protected abstract void AttachCartridgeUI(Control cartridgeUIFragment, string? title);

    /// <summary>
    /// The implementing ui needs to remove the passed ui from itself
    /// </summary>
    protected abstract void DetachCartridgeUI(Control cartridgeUIFragment);

    protected abstract void UpdateAvailablePrograms(List<(EntityUid, CartridgeComponent)> programs);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _activeUiFragment?.Dispose();
    }

    protected CartridgeComponent? RetrieveCartridgeComponent(EntityUid? cartridgeUid)
    {
        return _entityManager?.GetComponentOrNull<CartridgeComponent>(cartridgeUid);
    }

    private void SendCartridgeUiReadyEvent(EntityUid cartridgeUid)
    {
        var message = new CartridgeLoaderUiMessage(cartridgeUid, CartridgeUiMessageAction.UIReady);
        SendMessage(message);
    }

    private UIFragment? RetrieveCartridgeUI(EntityUid? cartridgeUid)
    {
        var component = _entityManager?.GetComponentOrNull<UIFragmentComponent>(cartridgeUid);
        component?.Ui?.Setup(this, cartridgeUid);
        return component?.Ui;
    }
}
