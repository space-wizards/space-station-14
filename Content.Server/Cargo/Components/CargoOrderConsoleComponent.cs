using Robust.Shared.Audio;

namespace Content.Server.Cargo.Components
{
    /// <summary>
    /// Handles sending order requests to cargo. Doesn't handle orders themselves via shuttle or telepads.
    /// </summary>
    [RegisterComponent]
    public sealed partial class CargoOrderConsoleComponent : Component
    {
        [DataField("soundError")] public SoundSpecifier ErrorSound =
            new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

        [DataField("soundConfirm")]
        public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
    }
}
