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
        public override string Name => "Listening";

        public void PassSpeechData(string speech, IEntity source, float distance)
        {
            
            foreach (var listener in Owner.GetAllComponents<IListen>())
            {
                if (distance > listener.GetListenRange()) { continue; }
                listener.HeardSpeech(speech, source);
            }
        }
    }
}
