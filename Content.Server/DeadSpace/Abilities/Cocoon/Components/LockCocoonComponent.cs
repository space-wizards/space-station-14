using Robust.Shared.Prototypes;
using Content.Shared.Mobs;

namespace Content.Server.DeadSpace.Abilities.Cocoon.Components;

[RegisterComponent]
public sealed partial class LockCocoonComponent : Component
{
    [DataField(required: true)]
    public string Cocoon;

    [DataField]
    public bool IsHumanoid = false;

    [DataField]
    public bool IgnorFriends = true;

    [DataField]
    public bool NeedHandcuff = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string HandcuffsProtorype = "Handcuffs";

    [DataField]
    public float Duration = 3f;

    [DataField]
    public EntProtoId LockCocoon = "ActionLockCocoon";

    [DataField, AutoNetworkedField]
    public EntityUid? LockCocoonEntity;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MobState> AllowedStates { get; set; } = new List<MobState>
    {
        MobState.Alive,
        MobState.Critical,
        MobState.Dead
    };
}
