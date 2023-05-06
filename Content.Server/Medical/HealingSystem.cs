using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Medical.Components;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stacks;
using Content.Server.Popups;
using Robust.Shared.Random;

namespace Content.Server.Medical;

public sealed class HealingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<DamageableComponent, HealingDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, DamageableComponent component, HealingDoAfterEvent args)
    {
        var dontRepeat = false;

        if (!TryComp(args.Used, out HealingComponent? healing))
            return;

        if (args.Handled || args.Cancelled)
            return;

        if (component.DamageContainerID is not null && !component.DamageContainerID.Equals(component.DamageContainerID))
            return;

        // Heal some bloodloss damage.
        if (healing.BloodlossModifier != 0)
        {
            if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
                return;
            var isBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount(uid, healing.BloodlossModifier);
            if (isBleeding != bloodstream.BleedAmount > 0)
            {
                dontRepeat = true;
                _popupSystem.PopupEntity(Loc.GetString("medical-item-stop-bleeding"), uid);
            }
        }

        // Restores missing blood
        if (healing.ModifyBloodLevel != 0)
            _bloodstreamSystem.TryModifyBloodLevel(uid, healing.ModifyBloodLevel);

        var healed = _damageable.TryChangeDamage(uid, healing.Damage, true, origin: args.Args.User);

        if (healed == null && healing.BloodlossModifier != 0)
            return;

        var total = healed?.Total ?? FixedPoint2.Zero;

        // Re-verify that we can heal the damage.
        _stacks.Use(args.Used.Value, 1);

        if (uid != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed {EntityManager.ToPrettyString(uid):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        _audio.PlayPvs(healing.HealingEndSound, uid, AudioHelpers.WithVariation(0.125f, _random).WithVolume(-5f));

        // Logic to determine the whether or not to repeat the healing action
        args.Repeat = (HasDamage(component, healing) && !dontRepeat);
        if (!args.Repeat && !dontRepeat)
            _popupSystem.PopupEntity(Loc.GetString("medical-item-finished-using", ("item", args.Used)), uid);
        args.Handled = true;
    }

    private bool HasDamage(DamageableComponent component, HealingComponent healing)
    {
        var damageableDict = component.Damage.DamageDict;
        var healingDict = healing.Damage.DamageDict;
        foreach (var type in healingDict)
        {
            if (damageableDict[type.Key].Value > 0)
            {
                return true;
            }
        }

        return false;
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
        if (!TryComp<DamageableComponent>(target, out var targetDamage))
            return false;

        if (component.DamageContainerID is not null &&
            !component.DamageContainerID.Equals(targetDamage.DamageContainerID))
            return false;

        if (user != target && !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return false;

        if (!TryComp<StackComponent>(uid, out var stack) || stack.Count < 1)
            return false;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return false;

        if (!HasDamage(targetDamage, component) && !(bloodstream.BloodSolution.Volume < bloodstream.BloodSolution.MaxVolume && component.ModifyBloodLevel > 0))
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-item-cant-use", ("item", uid)), uid);
            return false;
        }

        if (component.HealingBeginSound != null)
        {
            _audio.PlayPvs(component.HealingBeginSound, uid,
                AudioHelpers.WithVariation(0.125f, _random).WithVolume(-5f));
        }

        var isNotSelf = user != target;

        var delay = isNotSelf
            ? component.Delay
            : component.Delay * GetScaledHealingPenalty(user, component);

        var doAfterEventArgs =
            new DoAfterArgs(user, delay, new HealingDoAfterEvent(), target, target: target, used: uid)
            {
                //Raise the event on the target if it's not self, otherwise raise it on self.
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
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
        if (!TryComp<MobThresholdsComponent>(uid, out var mobThreshold) ||
            !TryComp<DamageableComponent>(uid, out var damageable))
            return output;
        if (!_mobThresholdSystem.TryGetThresholdForState(uid, MobState.Critical, out var amount, mobThreshold))
            return 1;

        var percentDamage = (float) (damageable.TotalDamage / amount);
        //basically make it scale from 1 to the multiplier.
        var modifier = percentDamage * (component.SelfHealPenaltyMultiplier - 1) + 1;
        return Math.Max(modifier, 1);
    }
}
