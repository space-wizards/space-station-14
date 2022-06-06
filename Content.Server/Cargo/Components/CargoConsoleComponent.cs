using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Sound;
using Content.Server.MachineLinking.Components;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components
{
    /// <summary>
    /// Handles sending order requests to cargo. Doesn't handle orders themselves via shuttle or telepads.
    /// </summary>
    [RegisterComponent]
    public sealed class CargoConsoleComponent : SharedCargoConsoleComponent
    {
        [DataField("requestOnly")]
        public bool _requestOnly = false;

        [DataField("errorSound")]
        public SoundSpecifier _errorSound = new SoundPathSpecifier("/Audio/Effects/error.ogg");

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CargoConsoleUiKey.Key);

        [DataField("senderPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string SenderPort = "OrderSender";
    }
}
