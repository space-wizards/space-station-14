using Robust.Shared.GameStates;

namespace Content.Shared.Sound.Components
{
    /// <summary>
    /// Simple sound emitter that emits sound on LandEvent
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class EmitSoundOnLandComponent : BaseEmitSoundComponent
    {
    }
}
