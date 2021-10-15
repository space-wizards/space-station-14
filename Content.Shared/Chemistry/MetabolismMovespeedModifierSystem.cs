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

        private readonly List<MovespeedModifierMetabolismComponent> _components = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentStartup>(AddComponent);
        }

        private void OnMovespeedHandleState(EntityUid uid, MovespeedModifierMetabolismComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MovespeedModifierMetabolismComponentState cast)
                return;

            if (EntityManager.TryGetComponent<MovementSpeedModifierComponent>(uid, out var modifier) &&
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

            for (var i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];

                if (component.Deleted)
                {
                    _components.RemoveAt(i);
                    continue;
                }

                if (component.ModifierTimer > currentTime) continue;

                _components.RemoveAt(i);
                EntityManager.RemoveComponent<MovespeedModifierMetabolismComponent>(component.Owner.Uid);

                if (component.Owner.TryGetComponent(out MovementSpeedModifierComponent? modifier))
                {
                    modifier.RefreshMovementSpeedModifiers();
                }
            }
        }
    }
}
