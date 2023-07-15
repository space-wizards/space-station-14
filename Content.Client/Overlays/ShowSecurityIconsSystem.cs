using Content.Shared.Access.Components;
using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    public sealed class ShowSecurityIconsSystem : ComponentAddedOverlaySystemBase<ShowSecurityIconsComponent>
    {
        [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        private Dictionary<string, StatusIconPrototype> _jobIcons = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        }

        private void OnGetStatusIconsEvent(EntityUid uid, BodyComponent _, ref GetStatusIconsEvent @event)
        {
            if (!IsActive)
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
    }
}
