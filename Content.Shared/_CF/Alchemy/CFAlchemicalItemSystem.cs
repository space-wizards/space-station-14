using Content.Shared.CF.Combat;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.CF.Alchemy;

/// <summary>
/// Activate alchemical / potion consumables (drink self or feed another).
/// Timing and drink sound follow SS14 drinks (Space Cola / <c>Edible</c> Drink).
/// </summary>
public sealed partial class CFAlchemicalItemSystem : EntitySystem
{
    private const float MaxFeedDistance = 1.5f;

    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private CFDyingSystem _dying = default!;
    [Dependency] private CFCombatBridgeSystem _bridge = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CFAlchemicalItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CFAlchemicalItemComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CFAlchemicalItemComponent, CFAlchemicalDrinkDoAfterEvent>(OnDrinkDoAfter);
    }

    private void OnUseInHand(Entity<CFAlchemicalItemComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartDrink(ent, args.User, args.User);
    }

    private void OnAfterInteract(Entity<CFAlchemicalItemComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryStartDrink(ent, args.User, target);
    }

    private bool TryStartDrink(Entity<CFAlchemicalItemComponent> item, EntityUid user, EntityUid target)
    {
        if (!HasComp<MobStateComponent>(target) && !HasComp<CFCombatantComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("cf-alchemy-drink-invalid"), user, user);
            return false;
        }

        var forceFeed = user != target;
        var delay = forceFeed ? item.Comp.ForceFeedDelay : item.Comp.Delay;

        // Same shape as IngestionSystem.GetEdibleDoAfterArgs — shows the progress bar while drinking.
        var args = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new CFAlchemicalDrinkDoAfterEvent(),
            eventTarget: item,
            target: target,
            used: item)
        {
            BreakOnHandChange = false,
            BreakOnMove = forceFeed,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = MaxFeedDistance,
            NeedHand = true,
            CancelDuplicate = true,
            // Progress bar visible to nearby players (cola drink bar).
            Hidden = false,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    private void OnDrinkDoAfter(Entity<CFAlchemicalItemComponent> item, ref CFAlchemicalDrinkDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        // Predicted like IngestionSystem.OnEdibleIngested (Space Cola drink.ogg).
        _audio.PlayPredicted(item.Comp.UseSound, target, args.User);

        if (!_net.IsServer)
            return;

        ApplyEffects(item, args.User, target);

        if (item.Comp.Consumable && !Deleted(item))
            QueueDel(item.Owner);
    }

    private void ApplyEffects(Entity<CFAlchemicalItemComponent> item, EntityUid user, EntityUid target)
    {
        if (TryComp(item, out CFHealConsumableComponent? heal))
            ApplyHeal(item, heal, user, target);
    }

    private void ApplyHeal(
        Entity<CFAlchemicalItemComponent> item,
        CFHealConsumableComponent heal,
        EntityUid user,
        EntityUid target)
    {
        _bridge.TryEnsureCombatTarget(target, out _, out _, out _);

        var amount = heal.HealBonus;
        for (var i = 0; i < heal.HealDice; i++)
            amount += _random.Next(1, heal.HealDie + 1);

        amount = Math.Max(0, amount);
        _dying.RestoreHitPoints(target, amount, user);

        var self = user == target;
        var msg = self
            ? Loc.GetString("cf-alchemy-heal-self", ("item", Identity.Entity(item, EntityManager)), ("amount", amount))
            : Loc.GetString("cf-alchemy-heal-other",
                ("user", Identity.Entity(user, EntityManager)),
                ("target", Identity.Entity(target, EntityManager)),
                ("item", Identity.Entity(item, EntityManager)),
                ("amount", amount));

        _popup.PopupEntity(msg, target, user);
        if (!self)
            _popup.PopupEntity(msg, target, target);
    }
}

[Serializable, NetSerializable]
public sealed partial class CFAlchemicalDrinkDoAfterEvent : SimpleDoAfterEvent;
