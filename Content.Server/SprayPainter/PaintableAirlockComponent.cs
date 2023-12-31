using Content.Shared.Roles;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.SprayPainter;

[RegisterComponent]
public sealed partial class PaintableAirlockComponent : Component
{
    /// <summary>
    /// Group of styles this airlock can be painted with, e.g. glass, standard or external.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AirlockGroupPrototype> Group = string.Empty;

    /// <summary>
    /// Department this airlock is painted as.
    /// Must be specified in prototypes for turf war to work.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department = string.Empty;
}
