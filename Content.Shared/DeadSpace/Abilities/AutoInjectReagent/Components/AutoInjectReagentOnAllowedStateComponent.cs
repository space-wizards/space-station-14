// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;

namespace Content.Shared.DeadSpace.Abilities.AutoInjectReagent.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoInjectReagentOnAllowedStateComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TimeUntilRegen;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<MobState> AllowedStates = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsReady = true;

    [DataField("durationRegenReagents")]
    public float DurationRegenReagents = 120f;

    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField("reagents", required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents { get; set; } = new Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>();
}
