using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body;

/// <summary>
/// Defines an organ that applies a sprite to the specified <see cref="Layer" /> within the body
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedVisualBodySystem))]
public sealed partial class VisualOrganComponent : Component
{
    /// <summary>
    /// The sprite layer on the entity that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The sprite data for the layer
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public PrototypeLayerData Data;

    /// <summary>
    /// When applying a profile, if the sex is present in this dictionary, overrides the state of the sprite data.
    /// Used for e.g. male vs female torsoes.
    /// </summary>
    [DataField]
    public Dictionary<Sex, string>? SexStateOverrides;

    /// <summary>
    /// The current profile data of this organ, used for alternate sprite selection and colouration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public OrganProfileData Profile = new();
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
    public Color EyeColor = Color.White;

    /// <summary>
    /// The "skin color" of this organ
    /// </summary>
    [DataField]
    public Color SkinColor = Color.White;
}

