using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Machines.Components;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class MachinePart
{
    /// <summary>
    /// Component type that is expected for this part to have
    /// to be considered a "Part" of the machine.
    /// </summary>
    [DataField(required: true)]
    public string Component = "";

    /// <summary>
    /// Expected offset to find this machine at.
    /// </summary>
    [DataField(required: true)]
    public Vector2i Offset;

    /// <summary>
    /// Sprite to show when user examines the machine and there
    /// is no matched entity for this part.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite = null;

    /// <summary>
    /// Expected rotation for this machine to have.
    /// </summary>
    [DataField]
    public Angle Rotation = Angle.Zero;

    /// <summary>
    /// Network entity associated with this part.
    /// Not null when an entity is successfully matched to the part and null otherwise.
    /// </summary>
    [DataField]
    public NetEntity? Entity = null;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultipartMachineComponent : Component
{
    /// <summary>
    /// Dictionary of unique names to specific parts of this machine
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, MachinePart> Parts = [];

    /// <summary>
    /// Determined orientation of this machine, used when displaying
    /// ghost entities to show machine part locations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle? Rotation = null;
}
