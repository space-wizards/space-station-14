using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using static Content.Shared.Chemistry.Components.MovespeedModifierMetabolismComponent;

namespace Content.Shared.Chemistry
{
    public class SharedMetabolismMovespeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
        }

        private void OnMovespeedHandleState(EntityUid uid, MovespeedModifierMetabolismComponent component, ComponentHandleState args)
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

            foreach (var component in ComponentManager.EntityQuery<MovespeedModifierMetabolismComponent>(true))
            {
                if (component.ModifierTimer.Value.End <= _gameTiming.CurTime)
                {
                    component.ModifierTimer = null;
                    component.WalkSpeedModifier = 1;
                    component.SprintSpeedModifier = 1;

                    if (component.Owner.TryGetComponent(out MovementSpeedModifierComponent? modifier))
                        modifier.RefreshMovementSpeedModifiers();
                    
                    component.Dirty();
                }
            }
        }
    }
}
