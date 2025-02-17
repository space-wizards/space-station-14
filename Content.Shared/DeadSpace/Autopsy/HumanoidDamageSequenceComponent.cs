// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Autopsy;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HumanoidDamageSequenceComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public HashSet<DamageEntry> DamageSequence = new HashSet<DamageEntry>();

    [DataField]
    public TimeSpan? TimeOfDeath = null;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DamageEntry
{
    [DataField]
    public TimeSpan TimeOfDamageTake { get; private set; } = default!;

    [DataField]
    public string DamageGroup { get; private set; } = default!;

    [DataField]
    public string DamageType { get; private set; } = default!;

    [DataField]
    public FixedPoint2 DamageTaken { get; private set; } = default!;

    public DamageEntry(TimeSpan timeOfDamageTake, string damageGroup, string damageType, FixedPoint2 damageTaken)
    {
        TimeOfDamageTake = timeOfDamageTake;
        DamageGroup = damageGroup;
        DamageType = damageType;
        DamageTaken = damageTaken;
    }
}
