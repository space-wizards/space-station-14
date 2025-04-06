using Robust.Shared.GameStates;

namespace Content.Shared.Matchstick.Components;

/// <summary>
///     Component for entities that light matches when they interact. (E.g. striking the match on the matchbox)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MatchboxComponent : Component;

