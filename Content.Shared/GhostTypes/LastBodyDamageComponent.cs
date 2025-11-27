using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.GhostTypes;

/// <summary>
/// Added to the Mind of an entity by the StoreDamageTakenOnMindSystem, allowing storage of the damage values their body had.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LastBodyDamageComponent : Component
{
    /// <summary>
    /// Dictionary DamageGroupPrototype proto ids to how much damage was received from that damage type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, FixedPoint2>? DamagePerGroup;

    /// <summary>
    /// Collection of possible damage types, stored by the StoreDamageTakenOnMind.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Special death cause that's saved after an event related to it is triggered
    /// For example, a BeforeExplodeEvent will save "Explosion" as the special cause of death
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SpecialCauseOfDeath;
}
