using Content.Shared.Interaction;
using Content.Shared.Juke;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedMidiJukeComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInteractHand(EntityUid uid, SharedMidiJukeComponent component, InteractHandEvent args)
        {
            throw new System.NotImplementedException();
        }

        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<MidiJukeComponent>(true))
            {
                var midiEvents = component.PlayTick();
                if (midiEvents.Count == 0) return;
                var uid = component.Owner.Uid;
                RaiseLocalEvent(uid, new MidiJukeMidiEventsMessage(midiEvents.ToArray()), true); //todo: handle this clientside
            }
        }
    }
}
