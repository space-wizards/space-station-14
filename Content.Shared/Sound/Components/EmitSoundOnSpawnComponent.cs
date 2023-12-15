using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components;

/// <summary>
///     Simple sound emitter that emits sound on entity spawn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmitSoundOnSpawnComponent : BaseEmitSoundComponent
{
}
