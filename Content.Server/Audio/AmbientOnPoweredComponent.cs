using Content.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.Server.Audio
{
    /// <summary>
    /// Toggles <see cref="AmbientSoundComponent"/> on when powered and off when not powered.
    /// </summary>
    [RegisterComponent]
    public sealed class AmbientOnPoweredComponent : Component
    {
    }
}
