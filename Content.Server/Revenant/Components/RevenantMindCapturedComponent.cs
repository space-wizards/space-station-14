// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Languages.Prototypes;
using Robust.Shared.Containers;
using Content.Shared.FixedPoint;

namespace Content.Server.Revenant.Components;

[RegisterComponent]
public sealed partial class RevenantMindCapturedComponent : Component
{
    public RevenantMindCapturedComponent(EntityUid revenant, FixedPoint2 deadThreshold, FixedPoint2 critThreshold)
    {
        RevenantUid = revenant;
        DeadThreshold = deadThreshold;
        CritThreshold = critThreshold;
    }

    [ViewVariables(VVAccess.ReadOnly)]
    public float Accumulator = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float DurationOfCapture = 300f;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid RevenantUid = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid TargetUid = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public string ReturnTTSPrototype = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<LanguagePrototype>> ReturnKnownLanguages = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<LanguagePrototype>> ReturnCantSpeakLanguages = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public BaseContainer RevenantContainer = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 DeadThreshold = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 CritThreshold = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool EndingCapture;
}
