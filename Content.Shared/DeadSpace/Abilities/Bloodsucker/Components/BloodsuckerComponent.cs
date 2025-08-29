// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Mobs;

namespace Content.Shared.DeadSpace.Abilities.Bloodsucker.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedBloodsuckerSystem))]
public sealed partial class BloodsuckerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MobState> AllowedStates { get; set; } = new List<MobState>
    {
        MobState.Alive,
        MobState.Critical,
        MobState.Dead
    };

    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "BloodAmount";

    [DataField]
    public EntProtoId ActionSuckBlood = "ActionSuckBlood";

    [DataField]
    public EntityUid? ActionSuckBloodEntity;

    [DataField]
    public SoundSpecifier? InjectSound = default;

    [DataField]
    public float MaxCountReagent = 100f;

    [DataField]
    public float CountReagent { get; internal set; } = 0;

    [DataField]
    public float Duration = 2f;

    [DataField]
    public float HowMuchWillItSuck = 20f;
}
