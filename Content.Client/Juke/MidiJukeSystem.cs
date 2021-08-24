using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Client.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<MidiJukeMidiEventsMessage>(OnMidiEvent);
        }

        private void OnMidiEvent(MidiJukeMidiEventsMessage msg, EntitySessionEventArgs args)
        {
            var uid = msg.EntityUid;
            var entity = EntityManager.GetEntity(uid);
            var component = entity.GetComponent<MidiJukeComponent>();
            component.PlayEvents(msg.MidiEvents);
        }
    }
}
