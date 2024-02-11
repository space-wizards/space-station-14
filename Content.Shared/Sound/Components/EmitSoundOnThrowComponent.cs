using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Simple sound emitter that emits sound on ThrowEvent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmitSoundOnThrowComponent : BaseEmitSoundComponent
{
}
