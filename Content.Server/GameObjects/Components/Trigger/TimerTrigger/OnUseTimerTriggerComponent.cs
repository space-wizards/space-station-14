using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Trigger;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Trigger.TimerTrigger
{
    [RegisterComponent]
    public class OnUseTimerTriggerComponent : Component, IUse
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public override string Name => "OnUseTimerTrigger";

        private float _delay = 0f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _delay, "delay", 0f);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

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
