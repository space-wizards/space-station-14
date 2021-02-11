#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Trigger;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Trigger.TimerTrigger
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component, IUse
    {
        public override string Name => "OnUseTimerTrigger";

        private float _delay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _delay, "delay", 0f);
        }

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
