using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using System.Collections.Generic;
using static Content.Shared.Chemistry.Components.MovespeedModifierMetabolismComponent;

namespace Content.Shared.Chemistry
{
    public class SharedMetabolismMovespeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly List<MovespeedModifierMetabolismComponent> _components = new();
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentStartup>(AddComponent);
        }

        private void OnMovespeedHandleState(EntityUid uid, MovespeedModifierMetabolismComponent component, ComponentHandleState args)
        {
            if (args.Current is not MovespeedModifierMetabolismComponentState cast)
                return;

            component.WalkSpeedModifier = cast.WalkSpeedModifier;
            component.SprintSpeedModifier = cast.SprintSpeedModifier;
            component.ModifierTimer = cast.ModifierTimer;
            
        }
        private void AddComponent(EntityUid uid, MovespeedModifierMetabolismComponent component, ComponentStartup args)
        {
            _components.Add(component);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            for (var i = _components.Count - 1; i >= 0; i--)
            {
                MovespeedModifierMetabolismComponent component = _components[i];

                if (component.Deleted)
                       _components.RemoveAt(i);

                if (component.ModifierTimer <= _gameTiming.CurTime)
                {                   
                    component.ModifierTimer = null;
                    component.WalkSpeedModifier = 1;
                    component.SprintSpeedModifier = 1;
                
                    component.Dirty();
                }
            }
        }
    }
}
