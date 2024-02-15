using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStationAISystem)), AutoGenerateComponentState]
public sealed partial class AIChassisComponent : Component
{
    /// <summary>
    /// Whether or not the AI is activated
    /// </summary>
    [DataField("activated"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Activated;

    #region Brain
    /// <summary>
    /// A whitelist for which entities count as valid brains
    /// </summary>
    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    /// <summary>
    /// The container ID for the brain
    /// </summary>
    [DataField("brainContainerId")]
    public string BrainContainerId = "borg_brain";

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot BrainContainer = default!;


    public EntityUid? BrainEntity => BrainContainer.ContainedEntity;
    #endregion
}
