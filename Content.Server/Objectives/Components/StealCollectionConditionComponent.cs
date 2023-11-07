using Content.Server.Objectives.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that you steal a certain item.
/// </summary>
[RegisterComponent, Access(typeof(StealCollectionConditionSystem))]
public sealed partial class StealCollectionConditionComponent : Component
{
    /// <summary>
    /// A group of items to be stolen
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string StealGroup;

    /// <summary>
    /// When enabled, disables generation of this target if there is no entity on the map (disable for objects that can be created mid-round).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool VerifyMapExistance = true;
    /// <summary>
    /// The minimum number of items you need to steal to fulfill a objective
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinCollectionSize = 3;

    /// <summary>
    /// The maximum number of items you need to steal to fulfill a objective
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxCollectionSize = 15;

    /// <summary>
    /// Target collection siez
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int CollectionSize;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string ObjectiveText;
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string DescriptionText;
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string DescriptionMultiplyText;
}
