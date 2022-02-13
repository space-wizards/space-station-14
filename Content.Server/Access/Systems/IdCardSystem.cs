using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.PDA;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Access.Systems
{
    public class IdCardSystem : SharedIdCardSystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IdCardComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<IdCardComponent, BeingMicrowavedEvent>(OnMicrowaved);
        }

        private void OnInit(EntityUid uid, IdCardComponent id, ComponentInit args)
        {
            id.OriginalOwnerName ??= EntityManager.GetComponent<MetaDataComponent>(id.Owner).EntityName;
            UpdateEntityName(uid, id);
        }

        private void OnMicrowaved(EntityUid uid, IdCardComponent component, BeingMicrowavedEvent args)
        {
            if (TryComp<AccessComponent>(uid, out var access))
            {
                // If they're unlucky, brick their ID
                if (_random.Prob(0.25f))
                {
                    _popupSystem.PopupEntity(Loc.GetString("id-card-component-microwave-bricked", ("id", uid)),
                        uid, Filter.Pvs(uid));
                    access.Tags.Clear();
                }
                else
                {
                    _popupSystem.PopupEntity(Loc.GetString("id-card-component-microwave-safe", ("id", uid)),
                        uid, Filter.Pvs(uid));
                }

                // Give them a wonderful new access to compensate for everything
                var random = _random.Pick(_prototypeManager.EnumeratePrototypes<AccessLevelPrototype>().ToArray());
                access.Tags.Add(random.ID);
            }
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
                EntityManager.GetComponent<MetaDataComponent>(id.Owner).EntityName = id.OriginalOwnerName;
                return;
            }

            var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

            var val = string.IsNullOrWhiteSpace(id.FullName)
                ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                    ("originalOwnerName", id.OriginalOwnerName),
                    ("jobSuffix", jobSuffix))
                : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                    ("fullName", id.FullName),
                    ("jobSuffix", jobSuffix));
            EntityManager.GetComponent<MetaDataComponent>(id.Owner).EntityName = val;
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
                TryGetIdCard(heldItem.Value, out idCard))
            {
                return true;
            }

            // check entity itself
            if (TryGetIdCard(uid, out idCard))
                return true;

            // check inventory slot?
            if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) && TryGetIdCard(idUid.Value, out idCard))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Attempt to get an id card component from an entity, either by getting it directly from the entity, or by
        ///     getting the contained id from a <see cref="PDAComponent"/>.
        /// </summary>
        public bool TryGetIdCard(EntityUid uid, [NotNullWhen(true)] out IdCardComponent? idCard)
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
