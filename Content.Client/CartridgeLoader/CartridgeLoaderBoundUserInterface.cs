using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader;


public abstract class CartridgeLoaderBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager? _entityManager = default!;

    private EntityUid? _activeProgram;
    private CartridgeUI? _activeCartridgeUI;
    private Control? _activeUiFragment;

    protected CartridgeLoaderBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
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

        if (control is not null)
            AttachCartridgeUI(control, comp?.ProgramName);

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

    protected List<(EntityUid, CartridgeComponent)> GetCartridgeComponents(List<EntityUid> programs)
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

    protected abstract void AttachCartridgeUI(Control cartridgeUI, string? title);

    protected abstract void DetachCartridgeUI(Control cartridgeUI);

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

    private CartridgeUI? RetrieveCartridgeUI(EntityUid? cartridgeUid)
    {
        var component = _entityManager?.GetComponentOrNull<CartridgeUiComponent>(cartridgeUid);
        component?.Ui?.Setup(this);
        return component?.Ui;
    }
}
