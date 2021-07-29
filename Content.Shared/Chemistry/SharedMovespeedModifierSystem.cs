using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using static Content.Shared.Chemistry.Components.MovespeedModifierMetabolismComponent;

namespace Content.Shared.Chemistry
{
    public class SharedMovespeedModifierSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ResetModifiersEvent>(ResetModifiers);
        }

        private void ResetModifiers(EntityUid uid, MovespeedModifierMetabolismComponent component, ResetModifiersEvent args)
        {
            component.WalkSpeedModifier = 1;
            component.SprintSpeedModifier = 1;
            component.ModifierTimer = null;
            var movement = component.Owner.GetComponent<MovementSpeedModifierComponent>();
            movement.RefreshMovementSpeedModifiers();
        }

        private void OnMovespeedHandleState(EntityUid uid, MovespeedModifierMetabolismComponent component, ComponentHandleState args)
        {
            if (args.Current is not MovespeedModifierMetabolismComponentState cast)
                return;

            component.WalkSpeedModifier = cast.WalkSpeedModifier;
            component.SprintSpeedModifier = cast.SprintSpeedModifier;
            component.ModifierTimer = cast.ModifierTimer;

            //If any of the modifers aren't synced to the movement modifier component, then refresh them, otherwise don't
            //Also I don't know if this is a good way to do a NAND gate in c#
            component.Owner.TryGetComponent(out MovementSpeedModifierComponent? movement);
            if (!(cast.WalkSpeedModifier.Equals(movement?.WalkSpeedModifier) & cast.SprintSpeedModifier.Equals(movement?.SprintSpeedModifier)))
                movement?.RefreshMovementSpeedModifiers();
            
            
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in ComponentManager.EntityQuery<MovespeedModifierMetabolismComponent>(true))
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
