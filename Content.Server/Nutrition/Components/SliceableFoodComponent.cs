using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.Components;

[RegisterComponent, Access(typeof(SliceableFoodSystem))]
public sealed partial class SliceableFoodComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Slice = string.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");

    /// <summary>
    /// Number of slices the food starts with.
    /// </summary>
    [DataField("count"), ViewVariables(VVAccess.ReadWrite)]
    public ushort TotalCount = 5;

    /// <summary>
    /// Number of slices left.
    /// </summary>
    [DataField("remainingCount"), ViewVariables(VVAccess.ReadWrite)]
    public ushort Count;
}
