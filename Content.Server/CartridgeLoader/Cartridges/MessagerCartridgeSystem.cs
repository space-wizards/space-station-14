using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class MessagerCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    private void OnUiReady(EntityUid uid, MessagerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var state = new MessagerCartridgeUiState();
        _cartridgeLoaderSystem.UpdateCartridgeUiState(args.Loader, state);
    }

    private void OnUiMessage(EntityUid uid, MessagerCartridgeComponent component, CartridgeMessageEvent args)
    {
        // Обработка сообщений от UI
    }
}