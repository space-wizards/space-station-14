using Content.Shared.Sound;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Traitor.Uplink.Components
{
    [RegisterComponent]
    public class UplinkComponent : Component
    {
        public override string Name => "Uplink";

        [ViewVariables]
        [DataField("buySuccessSound")]
        public SoundSpecifier BuySuccessSound = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");

        [ViewVariables]
        [DataField("insufficientFundsSound")]
        public SoundSpecifier InsufficientFundsSound  = new SoundPathSpecifier("/Audio/Effects/error.ogg");

        [ViewVariables] public UplinkAccount? UplinkAccount;
    }
}
