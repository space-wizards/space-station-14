using Robust.Shared.GameStates;

namespace Content.Shared.Hands.Components;

/// <summary>
/// An item with this component will not be involuntarily dropped, e.g. from slips/stuns.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventForcedThrowComponent : Component;
