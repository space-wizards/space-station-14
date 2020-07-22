using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class ListeningComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
#pragma warning restore 649

        public override string Name => "Listening";

        private ListeningSystem _listeningSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            _listeningSystem = _entitySystemManager.GetEntitySystem<ListeningSystem>();
            _listeningSystem.Subscribe(this);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            _listeningSystem.Unsubscribe(this);
        }

        public void PassSpeechData(string speech, IEntity source)
        {
            var attachedListeners = Owner.GetAllComponents<IListen>().ToArray();
            foreach (var listener in attachedListeners)
            {
                listener.HeardSpeech(speech, source);
            }
        }

        public int GetListenRange()
        {
            //TODO: implement a way to support multiple listeners on one entity
            //right now it just returns the same amount as voicerange, maybe change to return a list to iterate in chatmanager?
            return 7;
        }
    }
}
