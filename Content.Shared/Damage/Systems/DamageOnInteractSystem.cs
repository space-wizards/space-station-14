using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnInteractSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnInteractComponent, InteractHandEvent>(OnHandInteract);
    }

    /// <summary>
    /// Damages the user that interacts with the entity with an empty hand and
    /// plays a sound or pops up text in response. If the user does not have
    /// proper protection, the user will only be damaged and other interactions
    /// will be cancelled.
    /// </summary>
    /// <param name="entity">The entity being interacted with</param>
    /// <param name="args">Contains the user that interacted with the entity</param>
    private void OnHandInteract(Entity<DamageOnInteractComponent> entity, ref InteractHandEvent args)
    {
        if (!entity.Comp.IsDamageActive)
            return;

        var totalDamage = entity.Comp.Damage;

        if (!entity.Comp.IgnoreResistances)
        {
            // try to get damage on interact protection from either the inventory slots of the entity
            _inventorySystem.TryGetInventoryEntity<DamageOnInteractProtectionComponent>(args.User, out var protectiveEntity);

            // or checking the entity for  the comp itself if the inventory didn't work
            if (protectiveEntity.Comp == null && TryComp<DamageOnInteractProtectionComponent>(args.User, out var protectiveComp))
            {
                protectiveEntity = (args.User, protectiveComp);
            }

            // if protectiveComp isn't null after all that, it means the user has protection,
            // so let's calculate how much they resist
            if (protectiveEntity.Comp != null)
            {
                totalDamage = DamageSpecifier.ApplyModifierSet(totalDamage, protectiveEntity.Comp.DamageProtection);
            }
        }

        totalDamage = _damageableSystem.TryChangeDamage(args.User, totalDamage,  origin: args.Target);

        if (totalDamage != null && totalDamage.AnyPositive())
        {
            args.Handled = true;
            _adminLogger.Add(LogType.Damaged, $"{ToPrettyString(args.User):user} injured their hand by interacting with {ToPrettyString(args.Target):target} and received {totalDamage.GetTotal():damage} damage");
            _audioSystem.PlayPredicted(entity.Comp.InteractSound, args.Target, args.User);

            if (entity.Comp.PopupText != null)
                _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), args.User, args.User);
        }
    }

    public void SetIsDamageActiveTo(Entity<DamageOnInteractComponent> entity, bool mode)
    {
        if (entity.Comp.IsDamageActive == mode)
            return;

        entity.Comp.IsDamageActive = mode;
        Dirty(entity);
    }
}
