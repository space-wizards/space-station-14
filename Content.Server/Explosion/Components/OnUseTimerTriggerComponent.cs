#nullable enable
using System;
using Content.Shared.Interaction;
using Content.Shared.Trigger;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component, IUse
    {
        public override string Name => "OnUseTimerTrigger";

        [DataField("delay")]
        private float _delay = 0f;

        public void Trigger(IEntity user)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Primed);

            EntitySystem.Get<TriggerSystem>().HandleTimerTrigger(TimeSpan.FromSeconds(_delay), user, Owner);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            Trigger(eventArgs.User);
            return true;
        }
    }
}
