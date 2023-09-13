using Content.Shared.Renameable.Systems;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Renameable.Components;

/// <summary>
/// Lets an entity be renamed by using a tool on it, by default a multitool.
/// If it has a wire panel it must be opened first.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedRenameableSystem))]
public sealed partial class RenameableComponent : Component
{
    /// <summary>
    /// Tool quality needed to rename this entity.
    /// </summary>
    [DataField("quality", customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string Quality = "Pulsing";

    /// <summary>
    /// Maximum name length, once trimmed of whitespace but before suffix is applied.
    /// </summary>
    [DataField("maxLength"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxLength = 20;

    /// <summary>
    /// Suffix to append to the chosen name to use for the final entity name.
    /// If it already ends with the suffix nothing is changed.
    /// </summary>
    [DataField("suffix"), ViewVariables(VVAccess.ReadWrite)]
    public string? Suffix;
}
