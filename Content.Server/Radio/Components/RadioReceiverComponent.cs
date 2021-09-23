using System.Collections.Generic;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Radio.Components
{
    [RegisterComponent, Friend(typeof(RadioListenerSystem))]
    public class RadioReceiverComponent : Component
    {
        public override string Name => "RadioReceiver";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("channels")]
        public List<int> Channels = new(){ 1459 };

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("broadcastChannel")]
        public int BroadcastFrequency { get; set; } = 1459;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("listenRange")]
        public int ListenRange { get; private set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        ///     Whether this receiver should speak any radio messages it receives.
        /// </summary>
        [DataField("speakMessage")]
        public bool SpeakMessage = false;
    }
}
