using Content.Shared.Starlight;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NullLink;

[Prototype("roleRequirementPrototype")]
public sealed partial class RoleRequirementPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ulong[] Roles { get; set; } = [];

    // A loc message specifying which exact role is needed and where.

    [DataField(required: true)]
    public string Discord = default!;

    [DataField(required: true)]
    public string RolesLoc = default!;
}