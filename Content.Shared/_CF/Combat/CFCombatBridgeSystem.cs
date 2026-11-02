using Content.Shared.CF.Character;
using Content.Shared.CF.Preferences;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Resolves Pathfinder combat stats for any SS14 <see cref="DamageableComponent"/> entity:
/// explicit <see cref="CFCombatantComponent"/>, vanilla mobs as Commoners, or objects via materials.
/// </summary>
public sealed partial class CFCombatBridgeSystem : EntitySystem
{
    public static readonly ProtoId<CFMaterialPrototype> DefaultObjectMaterial = "Wood";

    [Dependency] private IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Ensures the target can be struck. Returns false if it cannot take CF combat damage.
    /// </summary>
    public bool TryEnsureCombatTarget(
        EntityUid target,
        out CFCombatantComponent combatant,
        out int hardness,
        out bool isObject)
    {
        hardness = 0;
        isObject = false;
        combatant = default!;

        // Already has CF combat stats (player, training dummy, previously struck target).
        if (TryComp(target, out CFCombatantComponent? existing))
        {
            combatant = existing;
            isObject = IsObjectTarget(target);
            if (isObject)
                hardness = ResolveMaterial(target).Hardness;
            return true;
        }

        if (!HasComp<DamageableComponent>(target))
            return false;

        if (HasComp<MobStateComponent>(target))
        {
            combatant = EnsureCommoner(target);
            return true;
        }

        // Unattended object / structure.
        isObject = true;
        var material = ResolveMaterial(target);
        hardness = material.Hardness;
        combatant = EnsureObjectCombatant(target, material);
        return true;
    }

    public bool IsObjectTarget(EntityUid target)
    {
        return HasComp<CFMaterialComponent>(target)
               || (!HasComp<MobStateComponent>(target) && HasComp<DamageableComponent>(target));
    }

    public CFMaterialPrototype ResolveMaterial(EntityUid target)
    {
        if (TryComp(target, out CFMaterialComponent? explicitMat)
            && _prototypes.TryIndex(explicitMat.Material, out CFMaterialPrototype? proto))
        {
            return proto;
        }

        string? modifierSet = null;
        if (TryComp(target, out DamageableComponent? damageable))
            modifierSet = damageable.DamageModifierSetId?.Id;

        string? container = null;
        if (TryComp(target, out InjurableComponent? injurable))
            container = injurable.DamageContainer?.Id;

        CFMaterialPrototype? best = null;
        foreach (var material in _prototypes.EnumeratePrototypes<CFMaterialPrototype>())
        {
            if (modifierSet != null && material.DamageModifierSets.Contains(modifierSet))
                return material;

            if (container != null && material.DamageContainers.Contains(container))
                best ??= material;
        }

        if (best != null)
            return best;

        if (modifierSet != null)
        {
            var id = modifierSet;
            if (ContainsAny(id, "Glass", "Window"))
                return IndexOrDefault("ThinGlass");
            if (ContainsAny(id, "Cardboard", "Cloth"))
                return IndexOrDefault("Cloth");
            if (ContainsAny(id, "Wood"))
                return IndexOrDefault("Wood");
            if (ContainsAny(id, "Rock", "Stone", "Brick"))
                return IndexOrDefault("Stone");
            if (ContainsAny(id, "Plasteel", "Plastitanium", "Structural"))
                return IndexOrDefault("StructureMetal");
            if (ContainsAny(id, "Steel", "Metal", "Iron"))
                return IndexOrDefault("IronSteel");
        }

        if (container != null)
        {
            if (container == "StructuralInorganic")
                return IndexOrDefault("StructureMetal");
            if (container is "Silicon" or "Shield")
                return IndexOrDefault("IronSteel");
            if (container == "Inorganic")
                return IndexOrDefault("Wood");
        }

        return IndexOrDefault(DefaultObjectMaterial);
    }

    private CFCombatantComponent EnsureCommoner(EntityUid target)
    {
        var combatant = EnsureComp<CFCombatantComponent>(target);
        combatant.Level = CFCommonerStats.Level;
        combatant.ArmorClass = CFCommonerStats.ArmorClass;
        combatant.MaxHp = CFCommonerStats.HitPoints;
        combatant.CurrentHp = CFCommonerStats.HitPoints;
        combatant.ClassId = null;
        Dirty(target, combatant);

        var abilities = EnsureComp<CFAbilityScoreComponent>(target);
        abilities.AbilityModifiers = new Dictionary<CFAbility, int>(CFCommonerStats.Modifiers);
        Dirty(target, abilities);

        return combatant;
    }

    private CFCombatantComponent EnsureObjectCombatant(EntityUid target, CFMaterialPrototype material)
    {
        var combatant = EnsureComp<CFCombatantComponent>(target);
        combatant.Level = 0;
        combatant.ArmorClass = CFCommonerStats.ObjectArmorClass;
        combatant.MaxHp = material.HitPoints;
        combatant.CurrentHp = material.HitPoints;
        combatant.ClassId = null;
        Dirty(target, combatant);

        var matComp = EnsureComp<CFMaterialComponent>(target);
        matComp.Material = material.ID;
        Dirty(target, matComp);

        return combatant;
    }

    private CFMaterialPrototype IndexOrDefault(ProtoId<CFMaterialPrototype> id)
    {
        if (_prototypes.TryIndex(id, out CFMaterialPrototype? proto))
            return proto;

        return _prototypes.Index(DefaultObjectMaterial);
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
