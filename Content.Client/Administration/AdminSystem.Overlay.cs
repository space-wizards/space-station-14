using Content.Client.Administration.Managers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.Administration
{
    public partial class AdminSystem
    {
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        private AdminNameOverlay _adminNameOverlay = default!;

        private void InitializeOverlay()
        {
            _adminNameOverlay = new AdminNameOverlay(this, _entityManager, _eyeManager, _resourceCache, _entityLookup);
            _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
        }

        private void ShutdownOverlay()
        {
            _adminManager.AdminStatusUpdated -= OnAdminStatusUpdated;
        }

        private void OnAdminStatusUpdated()
        {
            AdminOverlayOff();
        }

        public void AdminOverlayOn(BaseButton.ButtonEventArgs? _ = null)
        {
            if (!_overlayManager.HasOverlay<AdminNameOverlay>())
                _overlayManager.AddOverlay(_adminNameOverlay);
        }

        public void AdminOverlayOff(BaseButton.ButtonEventArgs? _ = null)
        {
            _overlayManager.RemoveOverlay<AdminNameOverlay>();
        }
    }
}
