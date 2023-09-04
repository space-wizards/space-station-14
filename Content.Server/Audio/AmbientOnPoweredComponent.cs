using Content.Shared.Audio;

namespace Content.Server.Audio
{
    /// <summary>
    /// Toggles <see cref="AmbientSoundComponent"/> on when powered and off when not powered.
    /// </summary>
    [RegisterComponent]
    public sealed partial class AmbientOnPoweredComponent : Component
    {
    }
}
