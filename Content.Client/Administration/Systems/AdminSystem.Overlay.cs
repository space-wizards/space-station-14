using Content.Client.Administration.Managers;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.Administration.Systems
{
    public sealed partial class AdminSystem
    {
        [Dependency] private IOverlayManager _overlayManager = default!;
        [Dependency] private IResourceCache _resourceCache = default!;
        [Dependency] private IClientAdminManager _adminManager = default!;
        [Dependency] private IEyeManager _eyeManager = default!;
        [Dependency] private EntityLookupSystem _entityLookup = default!;

        private AdminNameOverlay _adminNameOverlay = default!;

        public event Action? OverlayEnabled;
        public event Action? OverlayDisabled;

        private void InitializeOverlay()
        {
            _adminNameOverlay = new AdminNameOverlay(this, EntityManager, _eyeManager, _resourceCache, _entityLookup);
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

        public void AdminOverlayOn()
        {
            if (_overlayManager.HasOverlay<AdminNameOverlay>()) return;
            _overlayManager.AddOverlay(_adminNameOverlay);
            OverlayEnabled?.Invoke();
        }

        public void AdminOverlayOff()
        {
            _overlayManager.RemoveOverlay<AdminNameOverlay>();
            OverlayDisabled?.Invoke();
        }
    }
}
