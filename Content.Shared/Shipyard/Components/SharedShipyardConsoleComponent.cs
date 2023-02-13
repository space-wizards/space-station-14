using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Audio;

namespace Content.Shared.Shipyard.Components
{
    [NetworkedComponent]
    public abstract class SharedShipyardConsoleComponent : Component
    {
        [DataField("soundError")]
        public SoundSpecifier ErrorSound =
            new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

        [DataField("soundConfirm")]
        public SoundSpecifier ConfirmSound =
            new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");    
    }
}
