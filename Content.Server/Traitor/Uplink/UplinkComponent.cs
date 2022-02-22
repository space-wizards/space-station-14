using Content.Shared.Sound;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Traitor.Uplink.Components
{
    [RegisterComponent]
    public sealed class UplinkComponent : Component
    {
        [ViewVariables]
        [DataField("buySuccessSound")]
        public SoundSpecifier BuySuccessSound = new SoundPathSpecifier("/Audio/Effects/kaching.ogg");

        [ViewVariables]
        [DataField("insufficientFundsSound")]
        public SoundSpecifier InsufficientFundsSound  = new SoundPathSpecifier("/Audio/Effects/error.ogg");

        [DataField("activatesInHands")]
        public bool ActivatesInHands = false;

        [DataField("presetInfo")]
        public PresetUplinkInfo? PresetInfo = null;

        [ViewVariables] public UplinkAccount? UplinkAccount;

        [Serializable]
        [DataDefinition]
        public sealed class PresetUplinkInfo
        {
            [DataField("balance")]
            public int StartingBalance;
        }
    }
}
