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
        var users = new List<MessagerUserEntry>
        {
            new(1, "Captain"),
            new(2, "Engineer"),
            new(3, "Doctor")
        };
        var state = new MessagerCartridgeUiState(MessagerStatus.Connected, users);
        _cartridgeLoaderSystem.UpdateCartridgeUiState(args.Loader, state);
    }

    private void OnUiMessage(EntityUid uid, MessagerCartridgeComponent component, CartridgeMessageEvent args)
    {
        // Обработка сообщений от UI
    }
}
