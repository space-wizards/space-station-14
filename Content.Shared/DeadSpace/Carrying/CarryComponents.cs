// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Carrying;

public enum CarrySize : byte
{
    Small,
    Large,
}

public enum CarryStrength : byte
{
    SmallOnly,
    Any,
}

[RegisterComponent]
public sealed partial class CarrySizeComponent : Component
{
    [DataField]
    public CarrySize Size = CarrySize.Large;
}

[RegisterComponent]
public sealed partial class CarryStrengthComponent : Component
{
    [DataField]
    public CarryStrength Strength = CarryStrength.Any;
}

/// <summary>
/// Makes picking up targets in the configured mob states instantaneous.
/// </summary>
[RegisterComponent]
public sealed partial class InstantCriticalCarryComponent : Component
{
    [DataField]
    public HashSet<MobState> States =
    [
        MobState.PreCritical,
        MobState.Critical,
    ];
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CarryingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Carried;

    public readonly List<EntityUid> VirtualItems = new();

    [ViewVariables]
    public bool Stopping;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class CarriedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Carrier;

    [ViewVariables]
    public bool Stopping;

    [ViewVariables]
    public bool AddedBlockMovement;

    [ViewVariables]
    public bool? PreviousCanCollide;

    [ViewVariables]
    public bool ForcedDown;

    [ViewVariables]
    public bool WasStanding;

    [ViewVariables]
    public bool EscapeInProgress;

    [ViewVariables]
    public TimeSpan EscapeCompleteTime;

    [ViewVariables]
    public TimeSpan NextEscapePopupTime;
}

[Serializable, NetSerializable]
public sealed partial class CarryDoAfterEvent : SimpleDoAfterEvent
{
}
