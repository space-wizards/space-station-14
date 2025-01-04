using Content.Shared.AlternateDimension;
using Robust.Shared.Prototypes;

namespace Content.Server.AlternateDimension;

/// <summary>
/// Automatically creates an linked entity on the same coordinates in reality, or a specified alternate reality.
/// </summary>
[RegisterComponent]
public sealed partial class AlternateDimensionAutoPortalComponent : Component
{
    [DataField(required: true)]
    public EntProtoId OtherSidePortal;

    [DataField(required: true)]
    public ProtoId<AlternateDimensionPrototype> TargetDimension;
}
