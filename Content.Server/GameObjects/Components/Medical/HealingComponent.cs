using System.Collections.Generic;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    public class HealingComponent : Component, IAfterInteract
    {
        public override string Name => "Healing";

        public Dictionary<DamageType, int> Heal { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, h => h.Heal, "heal", new Dictionary<DamageType, int>());
        }

        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            if (!eventArgs.Target.TryGetComponent(out ISharedBodyManagerComponent body))
            {
                return;
            }

            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                return;
            }

            if (eventArgs.User != eventArgs.Target)
            {
                var interactionSystem = EntitySystem.Get<SharedInteractionSystem>();
                var from = eventArgs.User.Transform.MapPosition;
                var to = eventArgs.Target.Transform.MapPosition;
                bool Ignored(IEntity entity) => entity == eventArgs.User || entity == eventArgs.Target;
                var inRange = interactionSystem.InRangeUnobstructed(from, to, predicate: Ignored);

                if (!inRange)
                {
                    return;
                }
            }

            if (Owner.TryGetComponent(out StackComponent stack) &&
                !stack.Use(1))
            {
                return;
            }

            foreach (var (type, amount) in Heal)
            {
                body.ChangeDamage(type, -amount, true);
            }
        }
    }
}
