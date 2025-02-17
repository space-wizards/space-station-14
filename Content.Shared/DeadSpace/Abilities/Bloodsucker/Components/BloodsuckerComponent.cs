// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
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

    [DataField("actionSuckBlood", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionSuckBlood = "ActionSuckBlood";

    [DataField("actionSuckBloodEntity")]
    public EntityUid? ActionSuckBloodEntity;

    [DataField("injectSound")]
    public SoundSpecifier? InjectSound = default;

    [DataField("maxCountReagent")]
    public float MaxCountReagent = 100f;

    [DataField("countReagent")]
    public float CountReagent { get; internal set; } = 0;

    [DataField("duration")]
    public float Duration = 2f;

    [DataField("howMuchWillItSuck")]
    public float HowMuchWillItSuck = 20f;
}
