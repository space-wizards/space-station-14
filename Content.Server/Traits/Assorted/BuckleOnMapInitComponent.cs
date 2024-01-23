using Robust.Shared.Prototypes;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// Upon MapInit buckles the attached entity to a newly spawned prototype.
/// </summary>
[RegisterComponent, Access(typeof(BuckleOnMapInitSystem))]
public sealed partial class BuckleOnMapInitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public EntProtoId Prototype;
}
