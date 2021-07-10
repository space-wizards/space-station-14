using Content.Shared.PDA;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.PDA
{
    [RegisterComponent]
    public class PDAComponent : SharedPDAComponent
    {
        [ViewVariables]
        [DataField("buySuccessSound")]
        private SoundSpecifier BuySuccessSound { get; } = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");

        [ViewVariables]
        [DataField("insufficientFundsSound")]
        private SoundSpecifier InsufficientFundsSound { get; } = new SoundPathSpecifier("/Audio/Effects/error.ogg");

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            switch(message)
            {
                case PDAUplinkBuySuccessMessage:
                    if(BuySuccessSound.TryGetSound(out var buySuccessSound))
                        SoundSystem.Play(Filter.Local(), buySuccessSound, Owner, AudioParams.Default.WithVolume(-2f));
                    break;

                case PDAUplinkInsufficientFundsMessage:
                    if(InsufficientFundsSound.TryGetSound(out var insufficientFundsSound))
                        SoundSystem.Play(Filter.Local(), insufficientFundsSound, Owner, AudioParams.Default);
                    break;
            }
        }
    }
}
