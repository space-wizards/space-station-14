using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.GhostTypes;

/// <summary>
/// Added to the Mind of an entity by <see cref="StoreDamageTakenOnMindSystem"/>, allowing storage of the damage values their body had.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LastBodyDamageComponent : Component
{
    /// <summary>
    /// Damage taken from the last body to have <see cref="StoreDamageTakenOnMindComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Special death cause that's saved after an event related to it is triggered
    /// For example, a BeforeExplodeEvent will save "Explosion" as the special cause of death
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpecialCauseOfDeathPrototype>? SpecialCauseOfDeath = null;
}

/// <summary>
/// Prototype for special causes of death (such as "Explosion")
/// </summary>
[Prototype]
public sealed partial class SpecialCauseOfDeathPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = string.Empty;

    // Specifies the amount of possible sprites for a special cause of death
    // These values are set up in the special_cause_of_death_types.yml file
    [DataField]
    public int NumOfStates;
}
