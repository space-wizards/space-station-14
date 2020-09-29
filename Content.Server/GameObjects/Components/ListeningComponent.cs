using Content.Server.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

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
