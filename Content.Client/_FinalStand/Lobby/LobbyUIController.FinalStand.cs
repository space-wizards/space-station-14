using Content.Shared._FinalStand.Economy;
using Robust.Shared.GameObjects;

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController
{
    private int _fsPerkPoints;

    private void InitializeFinalStandWallet()
    {
        SubscribeNetworkEvent<WalletUpdatedEvent>(OnFSWalletUpdated);
    }

    private void OnFSWalletUpdated(WalletUpdatedEvent ev, EntitySessionEventArgs args)
    {
        _fsPerkPoints = ev.PerkPoints;
        UpdateFSPerkPoints();
    }

    private void UpdateFSPerkPoints()
    {
        PreviewPanel?.SetPerkPointsText(_fsPerkPoints);
    }
}
