// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.Abilities.ReleaseGasPerSecond.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReleaseGasPerSecondComponent : Component
{
    [DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    [DataField("nextEmitInfection", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmitInfection;

    [DataField("durationEmitInfection")]
    public float DurationEmitInfection = 10f;

    [DataField("molesInfectionPerDuration")]
    public float MolesInfectionPerDuration = 5f;

    [DataField("gasID", required: true)]
    public int GasID = 0;

}

[ByRefEvent]
public readonly record struct DomainGasEvent();
