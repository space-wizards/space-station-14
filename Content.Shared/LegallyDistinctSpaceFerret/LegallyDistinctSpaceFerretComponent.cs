using System.Threading;
using Content.Shared.Actions;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class LegallyDistinctSpaceFerretComponent : Component
{
    [DataField]
    public string RoleIntroSfx = "";

    [DataField]
    public ProtoId<AntagPrototype> AntagProtoId = "LegallyDistinctSpaceFerret";

    [DataField]
    public string RoleBriefing = "";

    [DataField]
    public string RoleGreeting = "";
}
