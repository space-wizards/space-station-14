using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Used to define a complicated condition that requires C#
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class FireModeCondition
{
    /// <summary>
    /// Determines whether or not a certain entity can change firemode.
    /// </summary>
    /// <returns>Whether or not the firemode can be changed</returns>
    public abstract bool Condition(FireModeConditionConditionArgs args);
}

public readonly record struct FireModeConditionConditionArgs(EntityUid Shooter, EntityUid? Weapon, BatteryWeaponFireMode? FireMode, IEntityManager EntityManager);