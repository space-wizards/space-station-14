using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using static Content.Shared.Chemistry.Components.MovespeedModifierMetabolismComponent;

namespace Content.Shared.Chemistry
{
    // TODO CONVERT THIS TO A STATUS EFFECT!!!!!!!!!!!!!!!!!!!!!!!!
    public sealed class MetabolismMovespeedModifierSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movespeed = default!;

        private readonly List<MovespeedModifierMetabolismComponent> _components = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentHandleState>(OnMovespeedHandleState);
            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, ComponentStartup>(AddComponent);
            SubscribeLocalEvent<MovespeedModifierMetabolismComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        }

        private void OnGetState(EntityUid uid, MovespeedModifierMetabolismComponent component, ref ComponentGetState args)
        {
            args.State = new MovespeedModifierMetabolismComponentState(
                component.WalkSpeedModifier,
                component.SprintSpeedModifier,
                component.ModifierTimer);
        }

        private void OnMovespeedHandleState(EntityUid uid, MovespeedModifierMetabolismComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not MovespeedModifierMetabolismComponentState cast)
                return;

            component.WalkSpeedModifier = cast.WalkSpeedModifier;
            component.SprintSpeedModifier = cast.SprintSpeedModifier;
            component.ModifierTimer = cast.ModifierTimer;
        }

        private void OnRefreshMovespeed(EntityUid uid, MovespeedModifierMetabolismComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
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
                EntityManager.RemoveComponent<MovespeedModifierMetabolismComponent>(component.Owner);

                _movespeed.RefreshMovementSpeedModifiers(component.Owner);
            }
        }
    }
}
