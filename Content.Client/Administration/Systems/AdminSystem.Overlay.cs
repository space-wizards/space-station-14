using Content.Client.Administration.Managers;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client.Administration.Systems
{
    public sealed partial class AdminSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        private AdminNameOverlay _adminNameOverlay = default!;

        public event Action? OverlayEnabled;
        public event Action? OverlayDisabled;

        private void InitializeOverlay()
        {
            _adminNameOverlay = new AdminNameOverlay(
                this,
                EntityManager,
                _eyeManager,
                _resourceCache,
                _entityLookup,
                _userInterfaceManager,
                _configurationManager);
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
