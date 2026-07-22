// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.TheCircle.Shuttles;

/// <summary>
/// Marks the primary Circle shuttle controlled by the Uni Ops game rule.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CirclePrimaryShuttleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool TimerStarted;

    [DataField, AutoNetworkedField]
    public bool Unlocked;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockAt;
}

/// <summary>
/// Marks the secondary Circle shuttle, which unlocks independently after round start.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CircleSecondaryShuttleComponent : Component
{
    [DataField]
    public TimeSpan UnlockDelay = TimeSpan.FromMinutes(10);

    [DataField, AutoNetworkedField]
    public bool TimerStarted;

    [DataField, AutoNetworkedField]
    public bool Unlocked;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockAt;
}
