using Robust.Shared.GameStates;

namespace Content.Shared.Mind;

/// <summary>
/// Given to minds that will persist with the visited entity once their current entity gets deleted.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PersistentVisitorComponent : Component;
