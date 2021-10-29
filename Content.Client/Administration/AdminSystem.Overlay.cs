using System.Collections.Generic;
using Content.Shared.Administration.Events;

namespace Content.Client.Administration
{
    public partial class AdminSystem
    {
        private AdminNameOverlay _adminNameOverlay = default!;

        private void InitializeOverlay()
        {
            _adminNameOverlay = new AdminNameOverlay(this, _entityManager, _eyeManager, _resourceCache, _entityLookup);
        }

        public void AdminOverlayOn()
        {
            if (!_overlayManager.HasOverlay<AdminNameOverlay>())
                _overlayManager.AddOverlay(_adminNameOverlay);
        }

        public void AdminOverlayOff()
        {
            _overlayManager.RemoveOverlay<AdminNameOverlay>();
        }

        private void UpdateOverlay(List<PlayerListChangedEvent.PlayerInfo> playerInfos)
        {
            _adminNameOverlay.UpdatePlayerInfo(playerInfos);
        }
    }
}
