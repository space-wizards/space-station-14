using Robust.Shared.GameStates;

namespace Content.Shared.Blocking;

/// <summary>
/// This component gets dynamically added to an Entity when they raise a shield via <see cref="BlockingSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HasRaisedShieldComponent : Component;
