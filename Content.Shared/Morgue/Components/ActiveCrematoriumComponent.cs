using Robust.Shared.GameStates;

namespace Content.Shared.Morgue.Components;

/// <summary>
/// Used to track actively cooking crematoriums.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveCrematoriumComponent : Component;
