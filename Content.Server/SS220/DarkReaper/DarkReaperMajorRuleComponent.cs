// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkReaper;

[RegisterComponent]
public sealed partial class DarkReaperMajorRuleComponent : Component
{
    [DataField]
    public EntProtoId RunePrototypeId = "DarkReaperRune";

    [DataField]
    public ProtoId<AntagPrototype> RoleProtoId = "DarkReaper";

    [DataField]
    public int MinPlayers = 30;

    public EntityUid? ReaperMind;
}
