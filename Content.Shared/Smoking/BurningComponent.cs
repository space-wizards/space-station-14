using Robust.Shared.GameStates;

namespace Content.Shared.Smoking;

/// <summary>
/// Marker component used to track active burning objects.
/// </summary>
/// <remarks>
/// Right now only smoking uses this, but flammable could use it as well in the future.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class BurningComponent : Component;
