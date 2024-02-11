using Content.Shared.Sound.Components;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Simple sound emitter that emits sound on AfterActivatableUIOpenEvent
    /// </summary>
    [RegisterComponent]
    public sealed partial class EmitSoundOnUIOpenComponent : BaseEmitSoundComponent
    {
    }
}
