using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Movement.Components;
using static Content.Shared.Chemistry.Components.MovespeedModifierMetabolismComponent;

namespace Content.Shared.Chemistry
{
    public class MetabolismMovespeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private readonly HashSet<MovespeedModifierMetabolismComponent> _components = new();

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

            if (ComponentManager.TryGetComponent<MovementSpeedModifierComponent>(uid, out var modifier) &&
                (!component.WalkSpeedModifier.Equals(cast.WalkSpeedModifier) ||
                !component.SprintSpeedModifier.Equals(cast.SprintSpeedModifier)))
            {
                modifier.RefreshMovementSpeedModifiers();
            }

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

            var currentTime = _gameTiming.CurTime;

            foreach (var component in _components.ToArray())
            {
                if (component.Deleted)
                {
                    _components.Remove(component);
                    continue;
                }

                if (component.ModifierTimer > currentTime) continue;

                _components.Remove(component);
                ComponentManager.RemoveComponent<MovespeedModifierMetabolismComponent>(component.Owner.Uid);

                if (component.Owner.TryGetComponent(out MovementSpeedModifierComponent? modifier))
                {
                    modifier.RefreshMovementSpeedModifiers();
                }
            }
        }
    }
}
