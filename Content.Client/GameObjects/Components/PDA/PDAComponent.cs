using Content.Shared.GameObjects.Components.PDA;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.PDA
{
    [RegisterComponent]
    public class PDAComponent : SharedPDAComponent
    {
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            switch(message)
            {
                case PDAUplinkBuySuccessMessage _ :
                    SoundSystem.Play(Filter.Local(), "/Audio/Effects/kaching.ogg", Owner, AudioParams.Default.WithVolume(-2f));
                    break;

                case PDAUplinkInsufficientFundsMessage _ :
                    SoundSystem.Play(Filter.Local(), "/Audio/Effects/error.ogg", Owner, AudioParams.Default);
                    break;

            }
        }


    }
}
