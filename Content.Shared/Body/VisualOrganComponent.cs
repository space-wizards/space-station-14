using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true)]
[Access(typeof(SharedVisualBodySystem))]
public sealed partial class VisualOrganComponent : Component
{
    /// <summary>
    /// The layer on the entity that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The data for the layer
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public PrototypeLayerData Data;

    [DataField, AutoNetworkedField]
    public OrganProfileData Profile;
}

/// <summary>
/// Defines the coloration, sex, etc. of organs
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct OrganProfileData
{
    /// <summary>
    /// The "sex" of this organ
    /// </summary>
    [DataField]
    public Sex Sex;

    /// <summary>
    /// The "eye color" of this organ
    /// </summary>
    [DataField]
    public Color EyeColor;

    /// <summary>
    /// The "skin color" of this organ
    /// </summary>
    [DataField]
    public Color SkinColor;
}

