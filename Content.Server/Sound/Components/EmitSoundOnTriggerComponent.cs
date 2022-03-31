using Content.Server.Explosion.EntitySystems;

namespace Content.Server.Sound.Components
{
    /// <summary>
    /// Whenever a <see cref="TriggerEvent"/> is run play a sound in PVS range.
    /// </summary>
    [RegisterComponent]
    public sealed class EmitSoundOnTriggerComponent : BaseEmitSoundComponent
    {
    }
}
