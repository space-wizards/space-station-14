using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadAssignRoleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Role;
}