using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Research.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedResearchSystem)), AutoGenerateComponentState]
public sealed partial class TechnologyDatabaseComponent : Component
{
    /// <summary>
    /// A main discipline that locks out other discipline technology past a certain tier.
    /// </summary>
    [AutoNetworkedField]
    [DataField("mainDiscipline", customTypeSerializer: typeof(PrototypeIdSerializer<TechDisciplinePrototype>))]
    public string? MainDiscipline;

    [AutoNetworkedField(true)]
    [DataField("currentTechnologyCards")]
    public List<string> CurrentTechnologyCards = new();

    /// <summary>
    /// Which research disciplines are able to be unlocked
    /// </summary>
    [AutoNetworkedField(true)]
    [DataField("supportedDisciplines", customTypeSerializer: typeof(PrototypeIdListSerializer<TechDisciplinePrototype>))]
    public List<string> SupportedDisciplines = new();

    /// <summary>
    /// The ids of all the technologies which have been unlocked.
    /// </summary>
    [AutoNetworkedField(true)]
    [DataField("unlockedTechnologies", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
    public List<string> UnlockedTechnologies = new();

    /// <summary>
    /// The ids of all the lathe recipes which have been unlocked.
    /// This is maintained alongside the TechnologyIds
    /// </summary>
    /// todo: if you unlock all the recipes in a tech, it doesn't count as unlocking the tech. sadge
    [AutoNetworkedField(true)]
    [DataField("unlockedRecipes", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
    public List<string> UnlockedRecipes = new();
}

/// <summary>
/// Event raised on the database whenever its
/// technologies or recipes are modified.
/// </summary>
/// <remarks>
/// This event is forwarded from the
/// server to all of it's clients.
/// </remarks>
[ByRefEvent]
public readonly record struct TechnologyDatabaseModifiedEvent;
