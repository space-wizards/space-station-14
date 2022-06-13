using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState.Components;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Medical;

public sealed class HealingSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<HealingCancelledEvent>(OnHealingCancelled);
        SubscribeLocalEvent<DamageableComponent, HealingCompleteEvent>(OnHealingComplete);
    }

    private void OnHealingComplete(EntityUid uid, DamageableComponent component, HealingCompleteEvent args)
    {
        if (TryComp<MobStateComponent>(uid, out var state) && state.IsDead())
            return;

        if (TryComp<StackComponent>(args.Component.Owner, out var stack) && stack.Count < 1) return;

        if (component.DamageContainerID is not null &&
            !component.DamageContainerID.Equals(component.DamageContainerID)) return;

        if (args.Component.BloodlossModifier != 0)
        {
            // Heal some bloodloss damage.
            _bloodstreamSystem.TryModifyBleedAmount(uid, args.Component.BloodlossModifier);
        }

        var healed = _damageable.TryChangeDamage(uid, args.Component.Damage, true);

        // Reverify that we can heal the damage.
        if (healed == null)
            return;

        _stacks.Use(args.Component.Owner, 1, stack);

        if (uid != args.User)
            _adminLogger.Add(LogType.Healed, $"{EntityManager.ToPrettyString(args.User):user} healed {EntityManager.ToPrettyString(uid):target} for {healed.Total:damage} damage");
        else
            _adminLogger.Add(LogType.Healed, $"{EntityManager.ToPrettyString(args.User):user} healed themselves for {healed.Total:damage} damage");

        if (args.Component.HealingEndSound != null)
        {
            SoundSystem.Play(args.Component.HealingEndSound.GetSound(), Filter.Pvs(uid, entityManager:EntityManager), uid, AudioHelpers.WithVariation(0.125f).WithVolume(-5f));
        }
    }

    private static void OnHealingCancelled(HealingCancelledEvent ev)
    {
        ev.Component.CancelToken = null;
    }

    private void OnHealingUse(EntityUid uid, HealingComponent component, UseInHandEvent args)
    {
        if (args.Handled) return;

        if (TryHeal(uid, args.User, args.User, component))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(EntityUid uid, HealingComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null) return;

        if (TryHeal(uid, args.User, args.Target.Value, component))
            args.Handled = true;
    }

    private bool TryHeal(EntityUid uid, EntityUid user, EntityUid target, HealingComponent component)
    {
        if (component.CancelToken != null)
        {
            return false;
        }

        if (TryComp<MobStateComponent>(target, out var state) && state.IsDead())
            return false;

        if (!TryComp<DamageableComponent>(target, out var targetDamage))
            return false;

        if (component.DamageContainerID is not null && !component.DamageContainerID.Equals(targetDamage.DamageContainerID))
            return false;

        if (user != target &&
            !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
        {
            return false;
        }

        if (TryComp<SharedStackComponent>(component.Owner, out var stack) && stack.Count < 1)
            return false;

        component.CancelToken = new CancellationTokenSource();

        if (component.HealingBeginSound != null)
        {
            SoundSystem.Play(component.HealingBeginSound.GetSound(), Filter.Pvs(uid, entityManager:EntityManager), uid, AudioHelpers.WithVariation(0.125f).WithVolume(-5f));
        }

        _doAfter.DoAfter(new DoAfterEventArgs(user, component.Delay, component.CancelToken.Token, target)
        {
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            // Didn't break on damage as they may be trying to prevent it and
            // not being able to heal your own ticking damage would be frustrating.
            BreakOnStun = true,
            NeedHand = true,
            TargetFinishedEvent = new HealingCompleteEvent
            {
                User = user,
                Component = component,
            },
            BroadcastCancelledEvent = new HealingCancelledEvent
            {
                Component = component,
            },
            // Juusstt in case damageble gets removed it avoids having to re-cancel the token. Won't need this when DoAfterEvent<T> gets added.
            PostCheck = () =>
            {
                component.CancelToken = null;
                return true;
            },
        });

        return true;
    }

    private sealed class HealingCompleteEvent : EntityEventArgs
    {
        public EntityUid User;
        public HealingComponent Component = default!;
    }

    private sealed class HealingCancelledEvent : EntityEventArgs
    {
        public HealingComponent Component = default!;
    }
}
