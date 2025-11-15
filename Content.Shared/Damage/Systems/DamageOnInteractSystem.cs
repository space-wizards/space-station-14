using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stunnable;

namespace Content.Shared.Damage.Systems;

public sealed class DamageOnInteractSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

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
        // Stop the interaction if the user attempts to interact with the object before the timer is finished
        if (_gameTiming.CurTime < entity.Comp.NextInteraction)
        {
            args.Handled = true;
            return;
        }

        if (!entity.Comp.IsDamageActive)
            return;

        var damageToApply = entity.Comp.Damage;

        if (!entity.Comp.IgnoreResistances)
        {
            DamageOnInteractProtectionComponent? protComp = null;

            if (_inventorySystem.TryGetSlotEntity(args.User, "gloves", out var gloveEnt) &&
                TryComp(gloveEnt, out DamageOnInteractProtectionComponent? invProt))
            {
                protComp = invProt;
            }
            else if (TryComp(args.User, out DamageOnInteractProtectionComponent? directProt))
            {
                protComp = directProt;
            }

            if (protComp != null)
                damageToApply = DamageSpecifier.ApplyModifierSet(damageToApply, protComp.DamageProtection);
        }

        var resultDamage = _damageableSystem.ChangeDamage(args.User, damageToApply, origin: args.Target);

        if (resultDamage.AnyPositive())
        {
            entity.Comp.LastInteraction = _gameTiming.CurTime;
            entity.Comp.NextInteraction = _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.InteractTimer);

            args.Handled = true;

            _adminLogger.Add(LogType.Damaged,
                $"{ToPrettyString(args.User):user} injured their hand by interacting with {ToPrettyString(args.Target):target} and received {resultDamage.GetTotal():damage} damage");

            _audioSystem.PlayPredicted(entity.Comp.InteractSound, args.Target, args.User);

            if (entity.Comp.PopupText != null)
                _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), args.User, args.User);

            if (_random.Prob(entity.Comp.StunChance))
                _stun.TryUpdateParalyzeDuration(args.User, TimeSpan.FromSeconds(entity.Comp.StunSeconds));
        }

        if (!entity.Comp.Throw ||
            !TryComp<PullableComponent>(entity, out var pullComp) ||
            pullComp.BeingPulled)
        {
            return;
        }

        _throwingSystem.TryThrow(entity, _random.NextVector2(), entity.Comp.ThrowSpeed, doSpin: true);
    }

    public void SetIsDamageActiveTo(Entity<DamageOnInteractComponent> entity, bool mode)
    {
        if (entity.Comp.IsDamageActive == mode)
            return;

        entity.Comp.IsDamageActive = mode;
        Dirty(entity);
    }
}
