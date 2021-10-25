using System.Linq;
using Content.Server.Inventory.Components;
using Content.Server.Storage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;

namespace Content.Server.Damage.Systems
{
    public sealed class DamageableSystem : SharedDamageableSystem
    {
        public override DamageSpecifier? TryChangeDamage(EntityUid uid, DamageSpecifier damage, bool ignoreResistances = false)
        {
            ApplyPassthroughDamage(uid, damage, ignoreResistances);

            return base.TryChangeDamage(uid, damage, ignoreResistances);
        }

        /// <summary>
        ///     When damaging an entity that contains other entities (e.g., a backpack), they can pass some of the
        ///     damage onto their contents.
        /// </summary>
        private void ApplyPassthroughDamage(EntityUid uid, DamageSpecifier originalDamage, bool ignoreResistances = false)
        {
            // only pass on damage if it is dealing damage. healing is not passed on.
            DamageSpecifier damage = new(originalDamage);
            damage.ClampMin(0);
            damage.TrimZeros();
            if (damage.Empty)
                return;

            if (EntityManager.TryGetComponent<InventoryComponent>(uid, out var inventory) && inventory.DamagePassthrough)
                ApplyPassthroughDamage(inventory, damage, ignoreResistances);

            if (EntityManager.TryGetComponent<SharedStorageComponent>(uid, out var storage) && storage.DamagePassthrough)
                ApplyPassthroughDamage(storage, damage, ignoreResistances);

            if (EntityManager.TryGetComponent<EntityStorageComponent>(uid, out var entityStorage) && entityStorage.DamagePassthrough)
                ApplyPassthroughDamage(entityStorage, damage, ignoreResistances);
        }

        /// <summary>
        ///     Like <see cref="ApplyPassthroughDamage"/>, but with 95% more code duplication!
        /// </summary>
        private void ApplyPassthroughDamage(InventoryComponent inventory, DamageSpecifier damage, bool ignoreResistances)
        {
            // modify damage before passing on
            if (!ignoreResistances && inventory.DamagePassthroughModifier != null &&
                _prototypeManager.TryIndex<DamageModifierSetPrototype>(inventory.DamagePassthroughModifier, out var mod))
            {
                damage = DamageSpecifier.ApplyModifierSet(damage, mod);
            }

            // apply damage to contained entities
            foreach (var slot in inventory.Slots)
            {
                var item = inventory.GetSlotItem(slot);
                if (item == null)
                    continue;

                TryChangeDamage(item.Owner.Uid, damage, ignoreResistances);
            }
        }

        /// <summary>
        ///     Like <see cref="ApplyPassthroughDamage"/>, but with 95% more code duplication!
        /// </summary>
        private void ApplyPassthroughDamage(SharedStorageComponent component, DamageSpecifier damage, bool ignoreResistances)
        {
            if (component.StoredEntities == null)
                return;

            // modify damage before passing on
            if (!ignoreResistances && component.DamagePassthroughModifier != null &&
                _prototypeManager.TryIndex<DamageModifierSetPrototype>(component.DamagePassthroughModifier, out var mod))
            {
                damage = DamageSpecifier.ApplyModifierSet(damage, mod);
            }

            // apply damage to contained entities
            foreach (var entity in component.StoredEntities.ToList())
            {
                TryChangeDamage(entity.Uid, damage);
            }
        }

        /// <summary>
        ///     Like <see cref="ApplyPassthroughDamage"/>, but with 95% more code duplication!
        /// </summary>
        private void ApplyPassthroughDamage(EntityStorageComponent component, DamageSpecifier damage, bool ignoreResistances)
        {
            // modify damage before passing on
            if (!ignoreResistances &&  component.DamagePassthroughModifier != null &&
                _prototypeManager.TryIndex<DamageModifierSetPrototype>(component.DamagePassthroughModifier, out var mod))
            {
                damage = DamageSpecifier.ApplyModifierSet(damage, mod);
            }

            // apply damage to contained entities
            foreach (var entity in component.Contents.ContainedEntities.ToList())
            {
                TryChangeDamage(entity.Uid, damage);
            }
        }
    }
}
