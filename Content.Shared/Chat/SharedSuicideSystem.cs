using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat;

public abstract class SharedSuicideSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> CannotSuicideTag = "CannotSuicide";
    private static readonly ProtoId<DamageTypePrototype> FallbackDamageType = "Blunt";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] protected readonly MobStateSystem MobStateSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] protected readonly TagSystem TagSystem = default!;

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
            Log.Error($"{nameof(SharedSuicideSystem)} could not find the damage type prototype associated with {damageType}. Falling back to {FallbackDamageType}");
            damagePrototype = _prototypeManager.Index(FallbackDamageType);
        }

        var damage = new DamageSpecifier(damagePrototype, lethalAmountOfDamage);
        _damageableSystem.ChangeDamage(target.AsNullable(), damage, true, origin: target);
    }

    public bool CanSuicide(EntityUid uid)
    {
        _mindSystem.GetMind(uid, out var mind);

        return mind is { PreventSuicide: false, PreventGhosting: false } && !MobStateSystem.IsDead(uid) && !HasComp<AdminFrozenComponent>(uid) &&
               TryComp<TagComponent>(uid, out var tag) && !TagSystem.HasTag(tag, CannotSuicideTag);
    }
}
