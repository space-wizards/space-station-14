using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using System;
using static Content.Shared.Chemistry.Components.SharedMovespeedModifierMetabolismComponent;

namespace Content.Shared.Chemistry
{
    public class SharedMovespeedModifierSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedMovespeedModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
            SubscribeLocalEvent<SharedMovespeedModifierMetabolismComponent, ResetModifiersEvent>(ResetModifiers);
        }

        private void ResetModifiers(EntityUid uid, SharedMovespeedModifierMetabolismComponent component, ResetModifiersEvent args)
        {
            component.WalkSpeedModifier = 1;
            component.SprintSpeedModifier = 1;
            component.ModifierTimer = null;
            var movement = component.Owner.GetComponent<MovementSpeedModifierComponent>();
            movement.RefreshMovementSpeedModifiers();
        }

        private void OnMovespeedHandleState(EntityUid uid, SharedMovespeedModifierMetabolismComponent component, ComponentHandleState args)
        {
            if (args.Current is not MovespeedModifierMetabolismComponentState cast)
                return;
            component.WalkSpeedModifier = cast.WalkSpeedModifier;
            component.SprintSpeedModifier = cast.SprintSpeedModifier;
            component.ModifierTimer = cast.ModifierTimer;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<SharedMovespeedModifierMetabolismComponent>(true))
            {
                component.Update(frameTime);
            }
        }
    }

    public class ResetModifiersEvent : EntityEventArgs
    {

        public ResetModifiersEvent()
        {

        }
    }
}
