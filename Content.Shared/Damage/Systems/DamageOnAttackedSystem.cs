using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
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
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;
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
        var totalDamage = entity.Comp.Damage;

        if (!entity.Comp.IgnoreResistances)
        {
            // try to get the damage on attacked protection component from something the entity has in their inventory
            _inventorySystem.TryGetInventoryEntity<DamageOnAttackedProtectionComponent>(args.User, out var protectiveEntity);

            // or checking the entity for the comp itself if the inventory didn't work
            if (protectiveEntity.Comp == null && TryComp<DamageOnAttackedProtectionComponent>(args.User, out var protectiveComp))
            {
                protectiveEntity = (args.User, protectiveComp);
            }

            // if protectiveComp isn't null after all that, it means the user has protection,
            // so let's calculate how much they resist
            if (protectiveEntity.Comp != null)
            {
                totalDamage -= protectiveEntity.Comp.DamageProtection;
                totalDamage.ClampMin(0); // don't let them heal just because they have enough protection
            }
        }

        totalDamage = _damageableSystem.TryChangeDamage(args.User, totalDamage, entity.Comp.IgnoreResistances, origin: entity);

        if (totalDamage != null && totalDamage.AnyPositive())
        {
            _adminLogger.Add(LogType.Damaged, $"{ToPrettyString(args.User):user} injured themselves by attacking {ToPrettyString(entity):target} and received {totalDamage.GetTotal():damage} damage");

            if (!_gameTiming.IsFirstTimePredicted || _net.IsServer)
                return;

            _audioSystem.PlayPvs(entity.Comp.InteractSound, entity);

            if (entity.Comp.PopupText != null)
                _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), args.User, args.User);

        }
    }
}
