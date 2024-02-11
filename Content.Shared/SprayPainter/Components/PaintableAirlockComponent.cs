using Content.Shared.Roles;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintableAirlockComponent : Component
{
    /// <summary>
    /// Group of styles this airlock can be painted with, e.g. glass, standard or external.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<AirlockGroupPrototype> Group = string.Empty;

    /// <summary>
    /// Department this airlock is painted as, or none.
    /// Must be specified in prototypes for turf war to work.
    /// To better catch any mistakes, you need to explicitly state a non-styled airlock has a null department.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DepartmentPrototype>? Department;
}
