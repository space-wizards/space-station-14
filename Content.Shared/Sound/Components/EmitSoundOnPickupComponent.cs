using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Simple sound emitter that emits sound on entity pickup
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmitSoundOnPickupComponent : BaseEmitSoundComponent;
