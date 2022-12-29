using Robust.Shared.GameStates;

namespace Content.Shared.DoAfter;

/// <summary>
///     Added to entities that are currently performing any doafters.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ActiveDoAfterComponent : Component
{

}
