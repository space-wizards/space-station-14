#nullable enable
using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Trigger;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Trigger.TimerTrigger
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component, IUse
    {
        public override string Name => "OnUseTimerTrigger";

        [YamlField("delay")]
        private float _delay = 0f;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
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
