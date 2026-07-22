// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.TheCircle.Engineering;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CircleRcdTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string FixtureId = "trigger";

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> IgnoredFactions = ["Necromorfs"];

    [DataField]
    public EntProtoId SpawnPrototype = "NecroKudzu";

    [DataField]
    public bool FlashTarget;

    [DataField]
    public float FlashRange = 1f;

    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public bool Triggered;
}
