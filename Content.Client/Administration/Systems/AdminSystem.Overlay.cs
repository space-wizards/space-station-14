using Content.Client.Administration.Managers;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration.Systems
{
    public sealed partial class AdminSystem
    {
        [Dependency] private IOverlayManager _overlayManager = default!;
        [Dependency] private IResourceCache _resourceCache = default!;
        [Dependency] private IClientAdminManager _adminManager = default!;
        [Dependency] private IEyeManager _eyeManager = default!;
        [Dependency] private EntityLookupSystem _entityLookup = default!;
        [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private IConfigurationManager _configurationManager = default!;
        [Dependency] private SharedRoleSystem _roles = default!;
        [Dependency] private IPrototypeManager _proto = default!;

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
                _configurationManager,
                _roles,
                _proto);
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
            if (_overlayManager.HasOverlay<AdminNameOverlay>())
                return;
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
