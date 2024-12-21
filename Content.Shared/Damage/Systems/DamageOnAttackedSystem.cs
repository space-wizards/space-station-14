using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnAttackedSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnAttackedComponent, AttackedEvent>(OnAttacked);
    }

    /// <summary>
    /// Damages the user that attacks the entity and potentially
    /// plays a sound or pops up text in response
    /// </summary>
    /// <param name="entity">The entity being hit</param>
    /// <param name="args">Contains the user that hit the entity</param>
    private void OnAttacked(Entity<DamageOnAttackedComponent> entity, ref AttackedEvent args)
    {
        if (!entity.Comp.IsDamageActive)
            return;

        var totalDamage = entity.Comp.Damage;

        if (!entity.Comp.IgnoreResistances)
        {
            // try to get the damage on attacked protection component from something the entity has in their inventory
            _inventorySystem.TryGetInventoryEntity<DamageOnAttackedProtectionComponent>(args.User, out var protectiveEntity);

            // if comp is null that means the user didn't have anything equipped that protected them
            // let's check their hands to see if the thing they attacked with gives them protection, like the GORILLA gauntlet
            if (protectiveEntity.Comp == null && TryComp<HandsComponent>(args.User, out var handsComp))
            {
                if (_handsSystem.TryGetActiveItem((args.User, handsComp), out var itemInHand) &&
                    TryComp<DamageOnAttackedProtectionComponent>(itemInHand, out var itemProtectComp)
                    && itemProtectComp.Slots == SlotFlags.NONE)
                {
                    protectiveEntity = (itemInHand.Value, itemProtectComp);
                }
            }

            // if comp is null, that means both the inventory and hands had nothing to protect them
            // let's check if the entity itself has the protective comp, like with borgs
            if (protectiveEntity.Comp == null &&
                TryComp<DamageOnAttackedProtectionComponent>(args.User, out var protectiveComp))
            {
                protectiveEntity = (args.User, protectiveComp);
            }

            // if comp is NOT NULL that means they have damage protection!
            if (protectiveEntity.Comp != null)
            {
                totalDamage = DamageSpecifier.ApplyModifierSet(totalDamage, protectiveEntity.Comp.DamageProtection);
            }
        }

        totalDamage = _damageableSystem.TryChangeDamage(args.User, totalDamage, entity.Comp.IgnoreResistances, origin: entity);

        if (totalDamage != null && totalDamage.AnyPositive())
        {
            _adminLogger.Add(LogType.Damaged, $"{ToPrettyString(args.User):user} injured themselves by attacking {ToPrettyString(entity):target} and received {totalDamage.GetTotal():damage} damage");
            _audioSystem.PlayPredicted(entity.Comp.InteractSound, entity, args.User);

            if (entity.Comp.PopupText != null)
                _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), args.User, args.User);

        }
    }

    public void SetIsDamageActiveTo(Entity<DamageOnAttackedComponent> entity, bool mode)
    {
        if (entity.Comp.IsDamageActive == mode)
            return;

        entity.Comp.IsDamageActive = mode;
        Dirty(entity);
    }
}
