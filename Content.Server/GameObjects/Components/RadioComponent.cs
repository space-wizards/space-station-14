using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    class RadioComponent : Component, IUse, IListen
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

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

            RadioOn = false;
        }

        public void PassOnMessage(string message)
        {
            if(RadioOn)
            {
                _radioSystem.SpreadMessage(Owner, message);
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
                Owner.PopupMessage(eventArgs.User, "The radio is now on.");
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, "The radio is now off.");
            }
            return true;
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
