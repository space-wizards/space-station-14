using Content.Shared.Cargo.Components;
using Content.Shared.Sound;

namespace Content.Server.Cargo.Components
{
    /// <summary>
    /// Handles sending order requests to cargo. Doesn't handle orders themselves via shuttle or telepads.
    /// </summary>
    [RegisterComponent]
    public sealed class CargoOrderConsoleComponent : SharedCargoConsoleComponent
    {
        [DataField("errorSound")]
        public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/error.ogg");
    }
}
