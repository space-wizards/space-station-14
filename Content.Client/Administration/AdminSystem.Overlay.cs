using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration
{
    public partial class AdminSystem
    {
        private AdminNameOverlay _adminNameOverlay = default!;

        private void InitializeOverlay()
        {
            _adminNameOverlay = new AdminNameOverlay(this, _entityManager, _eyeManager, _resourceCache, _entityLookup);
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
