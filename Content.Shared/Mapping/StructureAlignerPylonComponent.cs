using Robust.Shared.GameStates;

namespace Content.Shared.Mapping;

/// <summary>
/// This entity is used by StructureAlignerSystem as a reference point when determining the alignment of adjacent entities.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StructureAlignerPylonComponent : Component
{
    /// <summary>
    /// The types that will align to this entity.
    /// </summary>
    [DataField("pylons"), AutoNetworkedField]
    public StructureAlignerType AlignerPylonTypes = StructureAlignerType.Door;
}
