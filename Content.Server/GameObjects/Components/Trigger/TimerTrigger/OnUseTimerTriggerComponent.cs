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

namespace Content.Server.GameObjects.Components.Trigger.TimerTrigger
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component, IUse
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public override string Name => "OnUseTimerTrigger";

        [YamlField("delay")]
        private float _delay = 0f;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            var triggerSystem = _entitySystemManager.GetEntitySystem<TriggerSystem>();
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance)) {
                appearance.SetData(TriggerVisuals.VisualState, TriggerVisualState.Primed);
            }
            triggerSystem.HandleTimerTrigger(TimeSpan.FromSeconds(_delay), eventArgs.User, Owner);
            return true;
        }
    }
}
