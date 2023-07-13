using Content.Shared.Access.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.EntityHealthBar;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.EntityHealthHud
{
    public sealed class ShowSecurityIconsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        private bool _isActive = false;

        private Dictionary<string, StatusIconPrototype> _jobIcons = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowSecurityIconsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowSecurityIconsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowSecurityIconsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowSecurityIconsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
            SubscribeLocalEvent<BodyComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        }

        public void ApplyOverlays()
        {
            _isActive = true;
        }

        public void RemoveOverlay()
        {
            _isActive = false;
        }

        private void OnGetStatusIconsEvent(EntityUid uid, BodyComponent _, ref GetStatusIconsEvent @event)
        {
            if (!_isActive)
                return;

            var healthIcons = DecideSecurityIcon(uid);

            @event.StatusIcons.AddRange(healthIcons);
        }

        private IReadOnlyList<StatusIconPrototype> DecideSecurityIcon(EntityUid uid)
        {
            var result = new List<StatusIconPrototype>();
            if (_entManager.TryGetComponent<MetaDataComponent>(uid, out var metaDataComponent) &&
                metaDataComponent.Flags.HasFlag(MetaDataFlags.InContainer))
            {
                return result;
            }

            var iconToGet = "NoId";
            if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
            {
                // PDA
                if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda))
                {
                    iconToGet = pda.ContainedId?.JobTitle ?? string.Empty;
                }
                // ID Card
                else if (EntityManager.TryGetComponent(idUid, out IdCardComponent? id))
                {
                    iconToGet = id.JobTitle ?? string.Empty;
                }

                iconToGet = iconToGet.Replace(" ", "");
            }

            iconToGet = EnsureIcon(iconToGet, _jobIcons);
            result.Add(_jobIcons[iconToGet]);

            // Add arrest icons here, WYCI.

            return result;
        }

        private string EnsureIcon(string iconKey, Dictionary<string, StatusIconPrototype> icons)
        {
            if (!icons.ContainsKey(iconKey))
            {
                if (_prototypeMan.TryIndex<StatusIconPrototype>($"JobIcon_{iconKey}", out var securityIcon))
                {
                    icons.Add(iconKey, securityIcon);
                    return iconKey;
                }
            }
            else
            {
                return iconKey;
            }

            iconKey = "Unknown";
            return EnsureIcon(iconKey, icons);
        }

        private void OnInit(EntityUid uid, ShowSecurityIconsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                ApplyOverlays();
            }
        }

        private void OnRemove(EntityUid uid, ShowSecurityIconsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                RemoveOverlay();
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowSecurityIconsComponent component, PlayerAttachedEvent args)
        {
            ApplyOverlays();
        }

        private void OnPlayerDetached(EntityUid uid, ShowSecurityIconsComponent component, PlayerDetachedEvent args)
        {
            RemoveOverlay();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            RemoveOverlay();
        }
    }
}
