using Content.Server.Access.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
using Content.Shared.Access;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Access.Systems
{
    public class IdCardSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IdCardComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, IdCardComponent id, ComponentInit args)
        {
            id.OriginalOwnerName ??= id.Owner.Name;
            UpdateEntityName(uid, id);
        }

        public bool TryChangeJobTitle(EntityUid uid, string jobTitle, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            // TODO: Whenever we get admin logging these should be logged
            if (jobTitle.Length > SharedIdCardConsoleComponent.MaxJobTitleLength)
                jobTitle = jobTitle[..SharedIdCardConsoleComponent.MaxJobTitleLength];

            id.JobTitle = jobTitle;
            UpdateEntityName(uid, id);
            return true;
        }

        public bool TryChangeFullName(EntityUid uid, string fullName, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            if (fullName.Length > SharedIdCardConsoleComponent.MaxFullNameLength)
                fullName = fullName[..SharedIdCardConsoleComponent.MaxFullNameLength];

            id.FullName = fullName;
            UpdateEntityName(uid, id);
            return true;
        }

        /// <summary>
        /// Changes the <see cref="Entity.Name"/> of <see cref="Component.Owner"/>.
        /// </summary>
        /// <remarks>
        /// If either <see cref="FullName"/> or <see cref="JobTitle"/> is empty, it's replaced by placeholders.
        /// If both are empty, the original entity's name is restored.
        /// </remarks>
        private void UpdateEntityName(EntityUid uid, IdCardComponent? id = null)
        {
            if (!Resolve(uid, ref id))
                return;

            if (string.IsNullOrWhiteSpace(id.FullName) && string.IsNullOrWhiteSpace(id.JobTitle))
            {
                id.Owner.Name = id.OriginalOwnerName;
                return;
            }

            var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

            id.Owner.Name = string.IsNullOrWhiteSpace(id.FullName)
                ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                                ("originalOwnerName", id.OriginalOwnerName),
                                ("jobSuffix", jobSuffix))
                : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                                ("fullName", id.FullName),
                                ("jobSuffix", jobSuffix));
        }

        /// <summary>
        ///     Attempt to find an ID card on an entity. This will look in the entity itself, in the entity's hands, and
        ///     in the entity's inventory.
        /// </summary>
        public bool TryFindIdCard(EntityUid uid, [NotNullWhen(true)] out IdCardComponent? idCard)
        {
            // check held item?
            if (EntityManager.TryGetComponent(uid, out SharedHandsComponent? hands) &&
                hands.TryGetActiveHeldEntity(out var heldItem) &&
                TryGetIdCard(heldItem.Uid, out idCard))
            {
                return true;
            }

            // check entity itself
            if (TryGetIdCard(uid, out idCard))
                return true;

            // check inventory slot?
            if (EntityManager.TryGetComponent(uid, out InventoryComponent? inventoryComponent) &&
                inventoryComponent.HasSlot(EquipmentSlotDefines.Slots.IDCARD) &&
                inventoryComponent.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent? item) &&
                TryGetIdCard(item.Owner.Uid, out idCard))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Attempt to get an id card component from an entity, either by getting it directly from the entity, or by
        ///     getting the contained id from a <see cref="PDAComponent"/>.
        /// </summary>
        private bool TryGetIdCard(EntityUid uid, [NotNullWhen(true)] out IdCardComponent? idCard)
        {
            if (EntityManager.TryGetComponent(uid, out idCard))
                return true;

            if (EntityManager.TryGetComponent(uid, out PDAComponent? pda) && pda.ContainedID != null)
            {
                idCard = pda.ContainedID;
                return true;
            }

            return false;
        }
    }
}
