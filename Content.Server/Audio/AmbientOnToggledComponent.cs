using Content.Shared.Audio;

namespace Content.Server.Audio
{
    /// <summary>
    /// Toggles <see cref="AmbientSoundComponent"/> on when toggled and off when not toggled through ItemToggle.
    /// </summary>
    [RegisterComponent]
    public sealed partial class AmbientOnToggledComponent : Component
    {
    }
}
