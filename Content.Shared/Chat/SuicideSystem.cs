using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat;

public sealed class SuicideSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private static readonly ProtoId<TagPrototype> CannotSuicideTag = "CannotSuicide";
    private static readonly ProtoId<DamageTypePrototype> FallbackDamageType = "Blunt";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, SuicideEvent>(OnDamageableSuicide);
        SubscribeLocalEvent<MobStateComponent, SuicideEvent>(OnEnvironmentalSuicide);
    }

    /// <summary>
    /// Raise a SuicideAttemptEvent to catch any blockers. Returns true only if nothing is blocking.
    /// </summary>
    public bool CanSuicide(EntityUid victim)
    {
        var suicideAttempt = new SuicideAttemptEvent();
        RaiseLocalEvent(victim, suicideAttempt);

        return !suicideAttempt.Handled && !_tagSystem.HasTag(victim, CannotSuicideTag);
    }

    /// <summary>
    /// Calling this function will attempt to kill the user by suiciding on objects in the surrounding area
    /// or by applying a lethal amount of damage to the user with the default method.
    /// Used when writing /suicide
    /// </summary>
    public bool AttemptSuicide(EntityUid victim)
    {
        // Can't suicide if we're already dead
        if (!TryComp<MobStateComponent>(victim, out var mobState) || _mobStateSystem.IsDead(victim, mobState))
            return false;

        _adminLogger.Add(LogType.Mind, $"{ToPrettyString(victim):player} is attempting to suicide");

        ICommonSession? session = null;

        if (TryComp<ActorComponent>(victim, out var actor))
            session = actor.PlayerSession;

        if (!CanSuicide(victim))
            return false;

        var ev = new SuicideEvent(victim);
        RaiseLocalEvent(victim, ev);

        // Since the player is already dead the log will not contain their username.
        if (session != null)
            _adminLogger.Add(LogType.Mind, $"{session:player} suicided.");
        else
            _adminLogger.Add(LogType.Mind, $"{ToPrettyString(victim):player} suicided.");

        return true;
    }

    /// <summary>
    /// Applies lethal damage spread out across the damage types given.
    /// </summary>
    public void ApplyLethalDamage(Entity<DamageableComponent> target, DamageSpecifier damageSpecifier)
    {
        // Create a new damageSpecifier so that we don't make alterations to the original DamageSpecifier
        // Failing  to do this will permanently change a weapon's damage making it insta-kill people
        var appliedDamageSpecifier = new DamageSpecifier(damageSpecifier);
        if (!TryComp<MobThresholdsComponent>(target, out var mobThresholds))
            return;

        // Mob thresholds are sorted from alive -> crit -> dead,
        // grabbing the last key will give us how much damage is needed to kill a target from zero
        // The exact lethal damage amount is adjusted based on their current damage taken
        var lethalAmountOfDamage = mobThresholds.Thresholds.Keys.Last() - target.Comp.TotalDamage;
        var totalDamage = appliedDamageSpecifier.GetTotal();

        // Removing structural because it causes issues against entities that cannot take structural damage,
        // then getting the total to use in calculations for spreading out damage.
        appliedDamageSpecifier.DamageDict.Remove("Structural");

        // Split the total amount of damage needed to kill the target by every damage type in the DamageSpecifier
        foreach (var (key, value) in appliedDamageSpecifier.DamageDict)
        {
            appliedDamageSpecifier.DamageDict[key] = Math.Ceiling((double) (value * lethalAmountOfDamage / totalDamage));
        }

        _damageableSystem.ChangeDamage(target.AsNullable(), appliedDamageSpecifier, true, origin: target);
    }

    /// <summary>
    /// Applies lethal damage in a single type, specified by a single damage type.
    /// </summary>
    public void ApplyLethalDamage(Entity<DamageableComponent> target, ProtoId<DamageTypePrototype>? damageType)
    {
        if (!TryComp<MobThresholdsComponent>(target, out var mobThresholds))
            return;

        // Mob thresholds are sorted from alive -> crit -> dead,
        // grabbing the last key will give us how much damage is needed to kill a target from zero
        // The exact lethal damage amount is adjusted based on their current damage taken
        var lethalAmountOfDamage = mobThresholds.Thresholds.Keys.Last() - target.Comp.TotalDamage;

        // We don't want structural damage for the same reasons listed above
        if (!_prototypeManager.TryIndex(damageType, out var damagePrototype) || damagePrototype.ID == "Structural")
        {
            Log.Error($"{nameof(SuicideSystem)} could not find the damage type prototype associated with {damageType}. Falling back to {FallbackDamageType}");
            damagePrototype = _prototypeManager.Index(FallbackDamageType);
        }

        var damage = new DamageSpecifier(damagePrototype, lethalAmountOfDamage);
        _damageableSystem.ChangeDamage(target.AsNullable(), damage, true, origin: target);
    }

    /// <summary>
    /// Raise event to attempt to use held item, or surrounding entities to attempt to commit suicide
    /// </summary>
    private void OnEnvironmentalSuicide(Entity<MobStateComponent> victim, ref SuicideEvent args)
    {
        if (args.Handled || _mobStateSystem.IsCritical(victim))
            return;

        var suicideByEnvironmentEvent = new SuicideByEnvironmentEvent(victim);

        // Try to suicide by raising an event on the held item
        if (_hands.TryGetActiveItem(victim.Owner, out var item))
        {
            RaiseLocalEvent(item.Value, suicideByEnvironmentEvent);
            if (suicideByEnvironmentEvent.Handled)
            {
                args.Handled = suicideByEnvironmentEvent.Handled;
                return;
            }
        }

        // Try to suicide by nearby entities, like Microwaves or Crematoriums, by raising an event on it
        // Returns upon being handled by any entity
        var itemQuery = GetEntityQuery<ItemComponent>();
        foreach (var entity in _entityLookupSystem.GetEntitiesInRange(victim, 1, LookupFlags.Approximate | LookupFlags.Static))
        {
            // Skip any nearby items that can be picked up, we already checked the active held item above
            if (itemQuery.HasComponent(entity))
                continue;

            RaiseLocalEvent(entity, suicideByEnvironmentEvent);
            if (!suicideByEnvironmentEvent.Handled)
                continue;

            args.Handled = suicideByEnvironmentEvent.Handled;
            return;
        }
    }

    /// <summary>
    /// Default suicide behavior for any kind of entity that can take damage
    /// </summary>
    private void OnDamageableSuicide(Entity<DamageableComponent> victim, ref SuicideEvent args)
    {
        if (args.Handled)
            return;

        var othersMessage = Loc.GetString("suicide-command-default-text-others", ("name", Identity.Entity(victim, EntityManager)));
        _popup.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);

        var selfMessage = Loc.GetString("suicide-command-default-text-self");
        _popup.PopupEntity(selfMessage, victim, victim);

        args.DamageType ??= "Bloodloss";
        ApplyLethalDamage(victim, args.DamageType);
        args.Handled = true;
    }
}
