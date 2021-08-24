using System;
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
            SubscribeLocalEvent<MidiJukeComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInteractHand(EntityUid uid, MidiJukeComponent component, InteractHandEvent args)
        {
            Console.WriteLine("touch");
            if (!component.Playing)
            {
                var cast = (MidiJukeComponent) component;
                cast.Playing = true;
                cast.MidiFileName = "testmidi.mid";
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var component in ComponentManager.EntityQuery<MidiJukeComponent>(true))
            {
                if (!component.Playing) continue;
                var midiEvents = component.PlayTick();
                if (midiEvents.Count == 0) continue;
                var uid = component.Owner.Uid;
                Console.WriteLine($"Playing {midiEvents.Count} midiEvents.");
                RaiseNetworkEvent(new MidiJukeMidiEventsMessage(uid, midiEvents.ToArray()));
            }
        }
    }
}
