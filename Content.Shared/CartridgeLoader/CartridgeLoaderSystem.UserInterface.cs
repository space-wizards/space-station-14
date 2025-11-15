using System.Linq;

namespace Content.Shared.CartridgeLoader;

public sealed partial class CartridgeLoaderSystem
{
    private void OnLoaderUiMessage(Entity<CartridgeLoaderComponent> ent, ref CartridgeLoaderUiMessage message)
    {
        var cartridge = GetEntity(message.CartridgeUid);

        switch (message.Action)
        {
            case CartridgeUiMessageAction.Activate:
                ActivateProgram(ent, cartridge);
                break;
            case CartridgeUiMessageAction.Deactivate:
                DeactivateProgram(ent, cartridge);
                break;
            case CartridgeUiMessageAction.Install:
                InstallCartridge(ent, cartridge);
                break;
            case CartridgeUiMessageAction.Uninstall:
                UninstallProgram(ent, cartridge);
                break;
            case CartridgeUiMessageAction.UIReady:
                if (ent.Comp.ActiveProgram is { } foreground)
                {
                    var evt = new CartridgeUiReadyEvent(ent);
                    RaiseLocalEvent(foreground, ref evt);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized UI action passed from cartridge loader ui {message.Action}.");
        }
    }

    /// <summary>
    /// Relays ui messages meant for cartridges to the currently active cartridge
    /// </summary>
    private void OnUiMessage(Entity<CartridgeLoaderComponent> ent, ref CartridgeUiMessage args)
    {
        var cartridgeEvent = args.MessageEvent;
        cartridgeEvent.User = args.Actor;
        cartridgeEvent.LoaderUid = GetNetEntity(ent);
        cartridgeEvent.Actor = args.Actor;

        if (ent.Comp.ActiveProgram is { } foreground)
            RaiseLocalEvent(foreground, cartridgeEvent);
    }

    /// <summary>
    /// Updates the programs ui state
    /// </summary>
    /// <param name="loaderUid">The cartridge loaders entity uid</param>
    /// <param name="state">The programs ui state. Programs should use their own ui state class inheriting from <see cref="BoundUserInterfaceState"/></param>
    /// <param name="loader">The cartridge loader component</param>
    /// <remarks>
    /// This method is called "UpdateCartridgeUiState" but cartridges and a programs are the same. A cartridge is just a program as a visible item.
    /// </remarks>
    /// <seealso cref="Cartridges.NotekeeperCartridgeSystem.UpdateUiState"/>
    public void UpdateCartridgeUiState(EntityUid loaderUid, BoundUserInterfaceState state, CartridgeLoaderComponent? loader = null)
    {
        if (!Resolve(loaderUid, ref loader))
            return;

        if (_userInterface.HasUi(loaderUid, loader.UiKey))
            _userInterface.SetUiState(loaderUid, loader.UiKey, state);
    }


    /// <summary>
    /// Updates the cartridge loaders ui state.
    /// </summary>
    /// <remarks>
    /// Because the cartridge loader integrates with the ui of the entity using it, the entities ui state needs to inherit from <see cref="CartridgeLoaderUiState"/>
    /// and use this method to update its state so the cartridge loaders state can be added to it.
    /// </remarks>
    /// <seealso cref="PDA.PdaSystem.UpdatePdaUserInterface"/>
    public void UpdateUiState(Entity<CartridgeLoaderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_userInterface.HasUi(ent.Owner, ent.Comp.UiKey))
            return;

        var programs = GetNetEntityList(GetAllPrograms(ent).ToList());
        var state = new CartridgeLoaderUiState(programs, GetNetEntity(ent.Comp.ActiveProgram));
        _userInterface.SetUiState(ent.Owner, ent.Comp.UiKey, state);
    }
}
