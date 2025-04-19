using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Medical.Components;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Audio;

namespace Content.Server.Medical;

public sealed class HealingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealingComponent, UseInHandEvent>(OnHealingUse);
        SubscribeLocalEvent<HealingComponent, AfterInteractEvent>(OnHealingAfterInteract);
        SubscribeLocalEvent<DamageableComponent, HealingDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<DamageableComponent> entity, ref HealingDoAfterEvent args)
    {
        var dontRepeat = false;

        if (!TryComp(args.Used, out HealingComponent? healing))
            return;

        if (args.Handled || args.Cancelled)
            return;

        if (healing.DamageContainers is not null &&
            entity.Comp.DamageContainerID is not null &&
            !healing.DamageContainers.Contains(entity.Comp.DamageContainerID))
        {
            return;
        }

        // Heal some bloodloss damage.
        if (healing.BloodlossModifier != 0)
        {
            if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
                return;
            var isBleeding = bloodstream.BleedAmount > 0;
            _bloodstreamSystem.TryModifyBleedAmount(entity.Owner, healing.BloodlossModifier);
            if (isBleeding != bloodstream.BleedAmount > 0)
            {
                var popup = (args.User == entity.Owner)
                    ? Loc.GetString("medical-item-stop-bleeding-self")
                    : Loc.GetString("medical-item-stop-bleeding", ("target", Identity.Entity(entity.Owner, EntityManager)));
                _popupSystem.PopupEntity(popup, entity, args.User);
            }
        }

        // Restores missing blood
        if (healing.ModifyBloodLevel != 0)
            _bloodstreamSystem.TryModifyBloodLevel(entity.Owner, healing.ModifyBloodLevel);

        var healed = _damageable.TryChangeDamage(entity.Owner, healing.Damage * _damageable.UniversalTopicalsHealModifier, true, origin: args.Args.User);

        if (healed == null && healing.BloodlossModifier != 0)
            return;

        var total = healed?.GetTotal() ?? FixedPoint2.Zero;

        // Re-verify that we can heal the damage.

        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
        {
            _stacks.Use(args.Used.Value, 1, stackComp);

            if (_stacks.GetCount(args.Used.Value, stackComp) <= 0)
                dontRepeat = true;
        }
        else
        {
            QueueDel(args.Used.Value);
        }

        if (entity.Owner != args.User)
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed {EntityManager.ToPrettyString(entity.Owner):target} for {total:damage} damage");
        }
        else
        {
            _adminLogger.Add(LogType.Healed,
                $"{EntityManager.ToPrettyString(args.User):user} healed themselves for {total:damage} damage");
        }

        _audio.PlayPvs(healing.HealingEndSound, entity.Owner);

        // Logic to determine the whether or not to repeat the healing action
        args.Repeat = (HasDamage(entity, healing) && !dontRepeat);
        if (!args.Repeat && !dontRepeat)
            _popupSystem.PopupEntity(Loc.GetString("medical-item-finished-using", ("item", args.Used)), entity.Owner, args.User);
        args.Handled = true;
    }

    private bool HasDamage(Entity<DamageableComponent> ent, HealingComponent healing)
    {
        var damageableDict = ent.Comp.Damage.DamageDict;
        var healingDict = healing.Damage.DamageDict;
        foreach (var type in healingDict)
        {
            if (damageableDict[type.Key].Value > 0)
            {
                return true;
            }
        }

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            // Is ent missing blood that we can restore?
            if (healing.ModifyBloodLevel > 0
                && _solutionContainerSystem.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
                && bloodSolution.Volume < bloodSolution.MaxVolume)
            {
                return true;
            }

            // Is ent bleeding and can we stop it?
            if (healing.BloodlossModifier < 0 && bloodstream.BleedAmount > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void OnHealingUse(Entity<HealingComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryHeal(entity, args.User, args.User, entity.Comp))
            args.Handled = true;
    }

    private void OnHealingAfterInteract(Entity<HealingComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryHeal(entity, args.User, args.Target.Value, entity.Comp))
            args.Handled = true;
    }

    private bool TryHeal(EntityUid uid, EntityUid user, EntityUid target, HealingComponent component)
    {
        if (!TryComp<DamageableComponent>(target, out var targetDamage))
            return false;

        if (component.DamageContainers is not null &&
            targetDamage.DamageContainerID is not null &&
            !component.DamageContainers.Contains(targetDamage.DamageContainerID))
        {
            return false;
        }

        if (user != target && !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return false;

        if (TryComp<StackComponent>(uid, out var stack) && stack.Count < 1)
            return false;

        if (!HasDamage((target, targetDamage), component))
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-item-cant-use", ("item", uid)), uid, user);
            return false;
        }

        _audio.PlayPvs(component.HealingBeginSound, uid);

        var isNotSelf = user != target;

        if (isNotSelf)
        {
            var msg = Loc.GetString("medical-item-popup-target", ("user", Identity.Entity(user, EntityManager)), ("item", uid));
            _popupSystem.PopupEntity(msg, target, target, PopupType.Medium);
        }

        var delay = isNotSelf
            ? component.Delay
            : component.Delay * GetScaledHealingPenalty(user, component);

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, delay, new HealingDoAfterEvent(), target, target: target, used: uid)
            {
                // Didn't break on damage as they may be trying to prevent it and
                // not being able to heal your own ticking damage would be frustrating.
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
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
