using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class MessagerCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessagerCartridgeComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
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

    private void OnPacketReceived(EntityUid uid, MessagerCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        // Обработка входящих пакетов
    }

    /// <summary>
    ///     Getting user data from the IDcard
    /// </summary>
    public (int Id, string Name)? GetUserData(EntityUid loaderUid)
    {
        if (!TryComp<PdaComponent>(loaderUid, out var pda))
            return null;

        var idCardUid = pda.ContainedId;
        if (idCardUid == null)
            return null;

        if (!TryComp<IdCardComponent>(idCardUid, out var idCard))
            return null;

        var fullName = idCard.FullName;
        if (string.IsNullOrEmpty(fullName))
            fullName = "Unknown";

        var id = loaderUid.GetHashCode();
        return (id, fullName);
    }
}

