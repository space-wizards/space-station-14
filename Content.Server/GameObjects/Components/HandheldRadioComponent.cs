using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    [ComponentReference(typeof(IListen))]
    class HandheldRadioComponent : Component, IUse, IListen, IRadio
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;

        public override string Name => "Radio";

        private bool _radioOn;
        private int _listenRange = 7;
        private List<int> _channels = new List<int>();
        private int _broadcastChannel;
        private RadioSystem _radioSystem = default!;

        [ViewVariables]
        public bool RadioOn
        {
            get => _radioOn;
            private set
            {
                _radioOn = value;
                Dirty();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            _radioSystem = _entitySystemManager.GetEntitySystem<RadioSystem>();
            _channels.Add(1459);
            _broadcastChannel = 1459;

            RadioOn = false;
        }

        public void Speaker(string message)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            chat.EntitySay(Owner, message);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            RadioOn = !RadioOn;
            if(RadioOn)
            {
                _notifyManager.PopupMessage(Owner, eventArgs.User, "The radio is now on.");
            }
            else
            {
                _notifyManager.PopupMessage(Owner, eventArgs.User, "The radio is now off.");
            }
            return true;
        }

        public void HeardSpeech(string speech, IEntity source)
        {
            if (RadioOn)
            {
                Broadcast(speech, source);
            }
        }

        public int GetListenRange()
        {
            return _listenRange;
        }

        public void Receiver(string message, int channel, IEntity speaker)
        {
            if(RadioOn)
            {
                Speaker(message);
            }
        }

        public void Broadcast(string message, IEntity speaker)
        {
            _radioSystem.SpreadMessage(this, speaker, message, _broadcastChannel);
        }

        public List<int> GetChannels()
        {
            return _channels;
        }
    }
}
