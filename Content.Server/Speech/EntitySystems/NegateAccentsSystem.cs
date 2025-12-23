using Content.Server.Speech.Components;
using Content.Shared.Chat;
using Content.Shared.Implants;
using Content.Shared.Inventory;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class NegateAccentsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NegateAccentsComponent, TransformSpeechEvent>(OnTransformSpeech, before: [typeof(AccentSystem)]);
            SubscribeLocalEvent<NegateAccentsComponent, InventoryRelayedEvent<TransformSpeechEvent>>(OnTransformSpeechInventory, before: [typeof(AccentSystem)]);
            SubscribeLocalEvent<NegateAccentsComponent, ImplantRelayEvent<TransformSpeechEvent>>(OnTransformSpeechImplant, before: [typeof(AccentSystem)]);
        }

        private void TransformSpeech(Entity<NegateAccentsComponent> entity, TransformSpeechEvent args)
        {
            if (entity.Comp.CancelAccent)
                args.Cancel();
        }

        private void OnTransformSpeech(Entity<NegateAccentsComponent> entity, ref TransformSpeechEvent args)
        {
            TransformSpeech(entity, args);
        }

        private void OnTransformSpeechInventory(Entity<NegateAccentsComponent> entity, ref InventoryRelayedEvent<TransformSpeechEvent> args)
        {
            TransformSpeech(entity, args.Args);
        }

        private void OnTransformSpeechImplant(Entity<NegateAccentsComponent> entity, ref ImplantRelayEvent<TransformSpeechEvent> args)
        {
            TransformSpeech(entity, args.Event);
        }
    }
}
