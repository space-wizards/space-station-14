using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.MindShield;

[RegisterComponent]
public sealed partial class MindShieldDamageComponent : Component
{

    [DataField("mindShieldDamageGroup"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<DamageGroupPrototype> MindShieldDamageGroup = "Genetic";

    [DataField("mindShieldDamageAmount"), ViewVariables(VVAccess.ReadWrite)]
    public int MindShieldDamageAmount = 1000;
}
