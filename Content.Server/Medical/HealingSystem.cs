using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Random;

namespace Content.Server.Medical;

public sealed class HealingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<DamageableComponent, DoAfterEvent<HealingData>>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, DamageableComponent component, DoAfterEvent<HealingData> args)
    {
        if (args.Cancelled)
        {
            args.AdditionalData.HealingComponent.CancelToken = null;
            return;
        }

        if (args.Handled || args.Cancelled || _mobStateSystem.IsDead(uid) || args.Args.Used == null)
            return;

        if (component.DamageContainerID is not null && !component.DamageContainerID.Equals(component.DamageContainerID))
            return;

        // Heal some bloodloss damage.
        if (args.AdditionalData.HealingComponent.BloodlossModifier != 0)
            _bloodstreamSystem.TryModifyBleedAmount(uid, args.AdditionalData.HealingComponent.BloodlossModifier);

        var healed = _damageable.TryChangeDamage(uid, args.AdditionalData.HealingComponent.Damage, true, origin: args.Args.User);

        if (healed == null)
            return;

        // Reverify that we can heal the damage.
        _stacks.Use(args.Args.Used.Value, 1, args.AdditionalData.Stack);

        if (uid != args.Args.User)
            _adminLogger.Add(LogType.Healed, $"{EntityManager.ToPrettyString(args.Args.User):user} healed {EntityManager.ToPrettyString(uid):target} for {healed.Total:damage} damage");

        else
            _adminLogger.Add(LogType.Healed, $"{EntityManager.ToPrettyString(args.Args.User):user} healed themselves for {healed.Total:damage} damage");

        if (args.AdditionalData.HealingComponent.HealingEndSound != null)
            _audio.PlayPvs(args.AdditionalData.HealingComponent.HealingEndSound, uid, AudioHelpers.WithVariation(0.125f, _random).WithVolume(-5f));

        args.AdditionalData.HealingComponent.CancelToken = null;
        args.Handled = true;
    }

    private void OnHealingUse(EntityUid uid, HealingComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(uid, args.User, args.User, component))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(EntityUid uid, HealingComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(uid, args.User, args.Target.Value, component))
            args.Handled = true;
    }

    private bool TryHeal(EntityUid uid, EntityUid user, EntityUid target, HealingComponent component)
    {
        if (_mobStateSystem.IsDead(target) || !TryComp<DamageableComponent>(target, out var targetDamage) || component.CancelToken != null)
            return false;

        if (targetDamage.TotalDamage == 0)
            return false;

        if (component.DamageContainerID is not null && !component.DamageContainerID.Equals(targetDamage.DamageContainerID))
            return false;

        if (user != target && !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return false;

        if (!TryComp<StackComponent>(uid, out var stack) || stack.Count < 1)
            return false;

        if (component.HealingBeginSound != null)
            _audio.PlayPvs(component.HealingBeginSound, uid, AudioHelpers.WithVariation(0.125f, _random).WithVolume(-5f));

        var isNotSelf = user != target;

        var delay = isNotSelf
            ? component.Delay
            : component.Delay * GetScaledHealingPenalty(user, component);

        component.CancelToken = new CancellationTokenSource();

        var healingData = new HealingData(component, stack);

        var doAfterEventArgs = new DoAfterEventArgs(user, delay, cancelToken: component.CancelToken.Token,target: target, used: uid)
        {
            //Raise the event on the target if it's not self, otherwise raise it on self.
            RaiseOnTarget = isNotSelf,
            RaiseOnUser = !isNotSelf,
            BreakOnUserMove = true,
            BreakOnTargetMove = true,
            // Didn't break on damage as they may be trying to prevent it and
            // not being able to heal your own ticking damage would be frustrating.
            BreakOnStun = true,
            NeedHand = true,
            // Juusstt in case damageble gets removed it avoids having to re-cancel the token. Won't need this when DoAfterEvent<T> gets added.
            PostCheck = () => true
        };

        _doAfter.DoAfter(doAfterEventArgs, healingData);

        return true;
    }

    /// <summary>
    /// Scales the self-heal penalty based on the amount of damage taken
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public float GetScaledHealingPenalty(EntityUid uid, HealingComponent component)
    {
        var output = component.Delay;
        if (!TryComp<MobThresholdsComponent>(uid, out var mobThreshold) || !TryComp<DamageableComponent>(uid, out var damageable))
            return output;
        if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var amount, mobThreshold))
            return 1;

        var percentDamage = (float) (damageable.TotalDamage / amount);
        //basically make it scale from 1 to the multiplier.
        var modifier = percentDamage * (component.SelfHealPenaltyMultiplier - 1) + 1;
        return Math.Max(modifier, 1);
    }

    private record struct HealingData(HealingComponent HealingComponent, StackComponent Stack)
    {
        public HealingComponent HealingComponent = HealingComponent;
        public StackComponent Stack = Stack;
    }
}
