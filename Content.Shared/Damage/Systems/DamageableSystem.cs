using System.Linq;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Radiation.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Damage
{
    public sealed class DamageableSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly INetManager _netMan = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<DamageableComponent, ComponentInit>(DamageableInit);
            SubscribeLocalEvent<DamageableComponent, ComponentHandleState>(DamageableHandleState);
            SubscribeLocalEvent<DamageableComponent, ComponentGetState>(DamageableGetState);
            SubscribeLocalEvent<DamageableComponent, OnIrradiatedEvent>(OnIrradiated);
            SubscribeLocalEvent<DamageableComponent, RejuvenateEvent>(OnRejuvenate);
        }

        /// <summary>
        ///     Initialize a damageable component
        /// </summary>
        private void DamageableInit(EntityUid uid, DamageableComponent component, ComponentInit _)
        {
            if (component.DamageContainerID != null &&
                _prototypeManager.TryIndex<DamageContainerPrototype>(component.DamageContainerID,
                out var damageContainerPrototype))
            {
                // Initialize damage dictionary, using the types and groups from the damage
                // container prototype
                foreach (var type in damageContainerPrototype.SupportedTypes)
                {
                    component.Damage.DamageDict.TryAdd(type, FixedPoint2.Zero);
                }

                foreach (var groupID in damageContainerPrototype.SupportedGroups)
                {
                    var group = _prototypeManager.Index<DamageGroupPrototype>(groupID);
                    foreach (var type in group.DamageTypes)
                    {
                        component.Damage.DamageDict.TryAdd(type, FixedPoint2.Zero);
                    }
                }
            }
            else
            {
                // No DamageContainerPrototype was given. So we will allow the container to support all damage types
                foreach (var type in _prototypeManager.EnumeratePrototypes<DamageTypePrototype>())
                {
                    component.Damage.DamageDict.TryAdd(type.ID, FixedPoint2.Zero);
                }
            }

            component.DamagePerGroup = component.Damage.GetDamagePerGroup(_prototypeManager);
            component.TotalDamage = component.Damage.Total;
        }

        /// <summary>
        ///     Directly sets the damage specifier of a damageable component.
        /// </summary>
        /// <remarks>
        ///     Useful for some unfriendly folk. Also ensures that cached values are updated and that a damage changed
        ///     event is raised.
        /// </remarks>
        public void SetDamage(EntityUid uid, DamageableComponent damageable, DamageSpecifier damage)
        {
            damageable.Damage = damage;
            DamageChanged(uid, damageable);
        }

        /// <summary>
        ///     If the damage in a DamageableComponent was changed, this function should be called.
        /// </summary>
        /// <remarks>
        ///     This updates cached damage information, flags the component as dirty, and raises a damage changed event.
        ///     The damage changed event is used by other systems, such as damage thresholds.
        /// </remarks>
        public void DamageChanged(EntityUid uid, DamageableComponent component, DamageSpecifier? damageDelta = null,
            bool interruptsDoAfters = true, EntityUid? origin = null)
        {
            component.DamagePerGroup = component.Damage.GetDamagePerGroup(_prototypeManager);
            component.TotalDamage = component.Damage.Total;
            Dirty(component);

            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance) && damageDelta != null)
            {
                var data = new DamageVisualizerGroupData(component.DamagePerGroup.Keys.ToList());
                _appearance.SetData(uid, DamageVisualizerKeys.DamageUpdateGroups, data, appearance);
            }
            RaiseLocalEvent(uid, new DamageChangedEvent(component, damageDelta, interruptsDoAfters, origin));
        }

        /// <summary>
        ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
        /// </summary>
        /// <remarks>
        ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
        ///     function just applies the container's resistances (unless otherwise specified) and then changes the
        ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
        /// </remarks>
        /// <returns>
        ///     Returns a <see cref="DamageSpecifier"/> with information about the actual damage changes. This will be
        ///     null if the user had no applicable components that can take damage.
        /// </returns>
        public DamageSpecifier? TryChangeDamage(EntityUid? uid, DamageSpecifier damage, bool ignoreResistances = false,
            bool interruptsDoAfters = true, DamageableComponent? damageable = null, EntityUid? origin = null)
        {
            if (!uid.HasValue || !Resolve(uid.Value, ref damageable, false))
            {
                // TODO BODY SYSTEM pass damage onto body system
                return null;
            }

            if (damage.Empty)
            {
                return damage;
            }

            var before = new BeforeDamageChangedEvent(damage, origin);
            RaiseLocalEvent(uid.Value, ref before);

            if (before.Cancelled)
                return null;

            // Apply resistances
            if (!ignoreResistances)
            {
                if (damageable.DamageModifierSetId != null &&
                    _prototypeManager.TryIndex<DamageModifierSetPrototype>(damageable.DamageModifierSetId, out var modifierSet))
                {
                    damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
                }

                var ev = new DamageModifyEvent(damage, origin);
                RaiseLocalEvent(uid.Value, ev);
                damage = ev.Damage;

                if (damage.Empty)
                {
                    return damage;
                }
            }

            // Copy the current damage, for calculating the difference
            DamageSpecifier oldDamage = new(damageable.Damage);

            damageable.Damage.ExclusiveAdd(damage);
            damageable.Damage.ClampMin(FixedPoint2.Zero);

            var delta = damageable.Damage - oldDamage;
            delta.TrimZeros();

            if (!delta.Empty)
            {
                DamageChanged(uid.Value, damageable, delta, interruptsDoAfters, origin);
            }

            return delta;
        }

        /// <summary>
        ///     Sets all damage types supported by a <see cref="DamageableComponent"/> to the specified value.
        /// </summary>
        /// <remakrs>
        ///     Does nothing If the given damage value is negative.
        /// </remakrs>
        public void SetAllDamage(EntityUid uid, DamageableComponent component, FixedPoint2 newValue)
        {
            if (newValue < 0)
            {
                // invalid value
                return;
            }

            foreach (var type in component.Damage.DamageDict.Keys)
            {
                component.Damage.DamageDict[type] = newValue;
            }

            // Setting damage does not count as 'dealing' damage, even if it is set to a larger value, so we pass an
            // empty damage delta.
            DamageChanged(uid, component, new DamageSpecifier());
        }

        public void SetDamageModifierSetId(EntityUid uid, string damageModifierSetId, DamageableComponent? comp = null)
        {
            if (!Resolve(uid, ref comp))
                return;

            comp.DamageModifierSetId = damageModifierSetId;

            Dirty(comp);
        }

        private void DamageableGetState(EntityUid uid, DamageableComponent component, ref ComponentGetState args)
        {
            if (_netMan.IsServer)
            {
                args.State = new DamageableComponentState(component.Damage.DamageDict, component.DamageModifierSetId);
            }
            else
            {
                // avoid mispredicting damage on newly spawned entities.
                args.State = new DamageableComponentState(component.Damage.DamageDict.ShallowClone(), component.DamageModifierSetId);
            }
        }

        private void OnIrradiated(EntityUid uid, DamageableComponent component, OnIrradiatedEvent args)
        {
            var damageValue = FixedPoint2.New(args.TotalRads);

            // Radiation should really just be a damage group instead of a list of types.
            DamageSpecifier damage = new();
            foreach (var typeId in component.RadiationDamageTypeIDs)
            {
                damage.DamageDict.Add(typeId, damageValue);
            }

            TryChangeDamage(uid, damage);
        }

        private void OnRejuvenate(EntityUid uid, DamageableComponent component, RejuvenateEvent args)
        {
            TryComp<MobThresholdsComponent>(uid, out var thresholds);
            _mobThreshold.SetAllowRevives(uid, true, thresholds); // do this so that the state changes when we set the damage
            SetAllDamage(uid, component, 0);
            _mobThreshold.SetAllowRevives(uid, false, thresholds);
        }

        private void DamageableHandleState(EntityUid uid, DamageableComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not DamageableComponentState state)
            {
                return;
            }

            component.DamageModifierSetId = state.ModifierSetId;

            // Has the damage actually changed?
            DamageSpecifier newDamage = new() { DamageDict = new(state.DamageDict) };
            var delta = component.Damage - newDamage;
            delta.TrimZeros();

            if (!delta.Empty)
            {
                component.Damage = newDamage;
                DamageChanged(uid, component, delta);
            }
        }
    }

    /// <summary>
    ///     Raised before damage is done, so stuff can cancel it if necessary.
    /// </summary>
    [ByRefEvent]
    public record struct BeforeDamageChangedEvent(DamageSpecifier Delta, EntityUid? Origin = null, bool Cancelled = false);

    /// <summary>
    ///     Raised on an entity when damage is about to be dealt,
    ///     in case anything else needs to modify it other than the base
    ///     damageable component.
    ///
    ///     For example, armor.
    /// </summary>
    public sealed class DamageModifyEvent : EntityEventArgs, IInventoryRelayEvent
    {
        // Whenever locational damage is a thing, this should just check only that bit of armour.
        public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

        public readonly DamageSpecifier OriginalDamage;
        public DamageSpecifier Damage;
        public EntityUid? Origin;

        public DamageModifyEvent(DamageSpecifier damage, EntityUid? origin = null)
        {
            OriginalDamage = damage;
            Damage = damage;
            Origin = origin;
        }
    }

    public sealed class DamageChangedEvent : EntityEventArgs
    {
        /// <summary>
        ///     This is the component whose damage was changed.
        /// </summary>
        /// <remarks>
        ///     Given that nearly every component that cares about a change in the damage, needs to know the
        ///     current damage values, directly passing this information prevents a lot of duplicate
        ///     Owner.TryGetComponent() calls.
        /// </remarks>
        public readonly DamageableComponent Damageable;

        /// <summary>
        ///     The amount by which the damage has changed. If the damage was set directly to some number, this will be
        ///     null.
        /// </summary>
        public readonly DamageSpecifier? DamageDelta;

        /// <summary>
        ///     Was any of the damage change dealing damage, or was it all healing?
        /// </summary>
        public readonly bool DamageIncreased = false;

        /// <summary>
        ///     Does this event interrupt DoAfters?
        ///     Note: As provided in the constructor, this *does not* account for DamageIncreased.
        ///     As written into the event, this *does* account for DamageIncreased.
        /// </summary>
        public readonly bool InterruptsDoAfters = false;

        /// <summary>
        ///     Contains the entity which caused the change in damage, if any was responsible.
        /// </summary>
        public readonly EntityUid? Origin;

        public DamageChangedEvent(DamageableComponent damageable, DamageSpecifier? damageDelta, bool interruptsDoAfters, EntityUid? origin)
        {
            Damageable = damageable;
            DamageDelta = damageDelta;
            Origin = origin;

            if (DamageDelta == null)
                return;

            foreach (var damageChange in DamageDelta.DamageDict.Values)
            {
                if (damageChange > 0)
                {
                    DamageIncreased = true;
                    break;
                }
            }
            InterruptsDoAfters = interruptsDoAfters && DamageIncreased;
        }
    }
}
