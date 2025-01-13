using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// Enables a borg to disguise as another borg. This holds data about the disguise needed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBorgDisguiseSystem)), AutoGenerateComponentState]
public sealed partial class BorgDisguiseComponent : Component
{
    /// <summary>
    /// The entity needed to actually disguise. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntProtoId Action;

    [DataField]
    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The description to show when the entity is disguised.
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public EntProtoId Description;

    /// <summary>
    /// Whether the disguise is currently active.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public bool Disguised;

    #region Visuals

    /// <summary>
    /// The sprite state to use when the borg has a mind.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public string HasMindState;

    /// <summary>
    /// The sprite state to use when the borg has no mind.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public string NoMindState;

    #endregion
}
