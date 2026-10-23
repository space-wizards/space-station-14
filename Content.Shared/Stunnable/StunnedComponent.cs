using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

/// <summary>
/// This is used to temporarily prevent an entity from moving or acting.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStunSystem))]
public sealed partial class StunnedComponent : Component;
