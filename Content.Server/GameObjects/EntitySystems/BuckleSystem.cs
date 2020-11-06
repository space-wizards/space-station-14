using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.EntitySystems.Click;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BuckleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(InteractionSystem));
            UpdatesAfter.Add(typeof(InputSystem));
            SubscribeLocalEvent<MoveEvent>(MoveEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<MoveEvent>();
        }

        private void MoveEvent(MoveEvent ev)
        {
            if (ev.Sender.TryGetComponent(out BuckleComponent buckle))
            {
                buckle.OnMoveEvent(ev);
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var buckle in ComponentManager.EntityQuery<BuckleComponent>())
            {
                buckle.Update();
            }
        }
    }
}
