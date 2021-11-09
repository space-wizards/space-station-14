using Content.Server.Explosion.EntitySystems;
using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.MachineLinking.System
{
    public class TriggerOnSignalReceivedSystem : EntitySystem
    {
        [Dependency] private readonly TriggerSystem _trigger = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<TriggerOnSignalReceivedComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        private void OnSignalReceived(EntityUid uid, TriggerOnSignalReceivedComponent component, SignalReceivedEvent args)
        {
            _trigger.Trigger(EntityManager.GetEntity(uid));
        }
    }
}
