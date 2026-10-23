using Content.Shared.Construction;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class AppearanceChange : EntityEffectBase<AppearanceChange>
{
    /// <summary>
    /// The appearance key to use.
    /// </summary>
    [DataField]
    public Enum Key = ConstructionVisuals.Key;

    /// <summary>
    /// The enum data to set. If not specified, will set the data to the name of the current edges' target node
    /// (or the current node). This is because appearance changes are usually associated with reaching a new node.
    /// </summary>
    [DataField]
    public Enum? Data;
}
