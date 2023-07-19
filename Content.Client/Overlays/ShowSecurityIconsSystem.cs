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
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        }

        private void OnGetStatusIconsEvent(EntityUid uid, BodyComponent _, ref GetStatusIconsEvent @event)
        {
            if (!IsActive || @event.InContainer)
            {
                return;
            }

            var healthIcons = DecideSecurityIcon(uid);

            @event.StatusIcons.AddRange(healthIcons);
        }

        private IReadOnlyList<StatusIconPrototype> DecideSecurityIcon(EntityUid uid)
        {
            var result = new List<StatusIconPrototype>();

            var iconToGet = "NoId";
            if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
            {
                // PDA
                if (TryComp(idUid, out PdaComponent? pda))
                {
                    iconToGet = pda.ContainedId?.JobTitle ?? string.Empty;
                }
                // ID Card
                else if (TryComp(idUid, out IdCardComponent? id))
                {
                    iconToGet = id.JobTitle ?? string.Empty;
                }

                iconToGet = iconToGet.Replace(" ", "");
            }

            var icon = GetJobIcon(iconToGet);
            result.Add(icon);

            // Add arrest icons here, WYCI.

            return result;
        }

        private StatusIconPrototype GetJobIcon(string iconKey)
        {
            if (_prototypeMan.TryIndex<StatusIconPrototype>($"JobIcon_{iconKey}", out var securityIcon))
            {
                return securityIcon;
            }

            iconKey = "Unknown";
            return GetJobIcon(iconKey);
        }
    }
}
