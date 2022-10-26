using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class NotekeeperCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<NotekeeperCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void OnUiMessage(EntityUid uid, NotekeeperCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not NotekeeperUiMessageEvent message)
            return;

        if (message.Action == NotekeeperUiAction.Add)
        {
            component.Notes.Add(message.Note);
        }
        else
        {
            component.Notes.Remove(message.Note);
        }

        UpdateUiState(uid, args.LoaderUid, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, NotekeeperCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new NotekeeperUiState(component.Notes);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
