using Content.Server.Chemistry.Components;
using Content.Shared.Hands;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using System;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class InjectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InjectorComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<InjectorComponent, HandDeselectedEvent>(OnInjectorDeselected);
        }

        private void OnInjectorDeselected(EntityUid uid, InjectorComponent component, HandDeselectedEvent args)
        {
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }
        }

        private void OnSolutionChange(EntityUid uid, InjectorComponent component, SolutionChangedEvent args)
        {
            component.Dirty();
        }
    }
}
