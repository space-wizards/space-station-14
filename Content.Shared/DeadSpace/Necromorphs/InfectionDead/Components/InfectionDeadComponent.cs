// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class InfectionDeadComponent : Component
{
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    /// <summary>
    /// How long the Damage visual lasts
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("damageDuration", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DamageDuration = TimeSpan.FromSeconds(60);

    [DataField("nextDamageTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDamageTime = TimeSpan.Zero;
}

[ByRefEvent]

public readonly record struct InfectionDeadDamageEvent();

[ByRefEvent]
public readonly record struct InfectionNecroficationEvent();
