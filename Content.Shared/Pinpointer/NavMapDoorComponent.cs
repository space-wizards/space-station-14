using Robust.Shared.GameStates;

namespace Content.Shared.Pinpointer;

/// <summary>
/// This is used for objects which appear as doors on the navmap.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(NavMapSystem))]
public sealed partial class NavMapDoorComponent : Component;
