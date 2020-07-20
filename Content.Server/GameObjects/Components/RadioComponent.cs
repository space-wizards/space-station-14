using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    class RadioComponent : Component, IUse, IListen
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
#pragma warning restore 649

        public override string Name => "Radio";

        private bool _radioOn;
        private int _listenRange = 7;
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

            RadioOn = true;
            _radioSystem.Subscribe(this);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            if(RadioOn)
            {
                _radioSystem.Unsubscribe(this);
            }
        }

        public void PassOnMessage(string message)
        {
            if(RadioOn)
            {
                _radioSystem.SpreadMessage(this, message);
            }
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
                _radioSystem.Subscribe(this);
                return true;
            } else
            {
                _notifyManager.PopupMessage(Owner, eventArgs.User, "The radio is now off.");
                _radioSystem.Unsubscribe(this);
                return true;
            }
        }

        public void HeardSpeech(string speech, IEntity source)
        {
            PassOnMessage(speech);
        }

        public int GetListenRange()
        {
            return _listenRange;
        }
    }
}
