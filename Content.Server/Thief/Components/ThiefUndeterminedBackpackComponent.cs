

using Content.Shared.Thief;
using Robust.Shared.Prototypes;

namespace Content.Server.Thief.Components;

/// <summary>
/// This component stores the possible contents of the backpack,
/// which can be selected via the interface. After selecting the contents,
/// it is transformed into a certain other object
/// </summary>
[RegisterComponent]
public sealed partial class ThiefUndeterminedBackpackComponent : Component
{
    /// <summary>
    /// List of sets available for selection
    /// </summary>
    [DataField]
    public List<ProtoId<ThiefBackpackSetPrototype>> PossibleSets = new();

    [DataField]
    public List<int> SelectedSets = new();

    /// <summary>
    /// The backpack will try to turn into this object, and fill it with the selected gear.
    /// </summary>
    [DataField]
    public ProtoId<EntityPrototype> TransformAfterSelect = default!;
}
