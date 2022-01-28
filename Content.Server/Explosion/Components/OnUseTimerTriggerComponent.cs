using System;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Trigger;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component, IUse
    {
        public override string Name => "OnUseTimerTrigger";

        [DataField("delay")]
        private float _delay = 0f;

        // TODO: Need to split this out so it's a generic "OnUseTimerTrigger" component.
        public void Trigger(EntityUid user)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AppearanceComponent? appearance))
                appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Primed);

            EntitySystem.Get<TriggerSystem>().HandleTimerTrigger(TimeSpan.FromSeconds(_delay), Owner, user);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Trigger(eventArgs.User);
            return true;
        }
    }
}
