using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    public string Description;

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
    [DataField(required: true)]
    [AutoNetworkedField]
    public string HasMindState;

    /// <summary>
    /// The sprite state to use when the borg has no mind.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public string NoMindState;

    /// <summary>
    /// The sprite state to use for the borg's flashlight when disguised.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public string DisguisedLight;

    /// <summary>
    /// The sprite state to use for the borg's flashlight when undisguised.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public string RealLight;

    /// <summary>
    /// The color of the light when the borg is disguised.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public Color DisguisedLightColor = Color.White;

    #endregion
}

[Serializable, NetSerializable]
public enum BorgDisguiseVisuals : byte
{
    /// <summary>
    /// Whether the borg has a disguise activated.
    /// </summary>
    IsDisguised,
}

/// <summary>
/// Visual layers used when the borg is disguised.
/// </summary>
[Serializable, NetSerializable]
public enum BorgDisguiseVisualLayers : byte
{
    /// <summary>
    /// Main borg body layer.
    /// </summary>
    Body,

    /// <summary>
    /// Layer for the borg's mind state.
    /// </summary>
    Light,

    /// <summary>
    /// Layer for the borg flashlight status.
    /// </summary>
    LightStatus,
}
