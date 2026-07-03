using Content.Shared.Ninja.Systems;
using Content.Shared.Objectives.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for toggling glove powers.
/// </summary>
/// <remarks>
/// Requires <c>ItemToggleComponent</c>.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNinjaGlovesSystem))]
public sealed partial class NinjaGlovesComponent : Component
{
    /// <summary>
    /// Entity of the ninja using these gloves, usually means enabled
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// Abilities to give to the user when enabled.
    /// </summary>
    [DataField(required: true)]
    public List<NinjaGloveAbility> Abilities = new();
}

/// <summary>
/// An ability that adds components to the user when the gloves are enabled.
/// </summary>
[DataRecord]
public partial record struct NinjaGloveAbility()
{
    /// <summary>
    /// If not null, checks if an objective with this prototype has been completed.
    /// If it has, the ability components are skipped to prevent doing the objective twice.
    /// The objective must have <c>CodeConditionComponent</c> to be checked.
    /// </summary>
    [DataField]
    public EntProtoId<ObjectiveComponent>? Objective;

    /// <summary>
    /// Components to add and remove.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}
