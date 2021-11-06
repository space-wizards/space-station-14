using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class MicrowaveSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MicrowaveComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, MicrowaveComponent component, SolutionChangedEvent args)
        {
            component.DirtyUi();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var comp in EntityManager.EntityQuery<MicrowaveComponent>())
            {
                comp.OnUpdate();
            }
        }
    }
}
