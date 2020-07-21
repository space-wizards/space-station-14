using Content.Server.GameObjects.Components.Buckle;
using Content.Server.GameObjects.EntitySystems.Click;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class BuckleSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        /// <summary>
        ///     Checks if a buckled entity should be unbuckled from moving
        ///     too far from its strap.
        /// </summary>
        /// <param name="moveEvent">The move event of a buckled entity.</param>
        private void MoveEvent(MoveEvent moveEvent)
        {
            if (!moveEvent.Sender.TryGetComponent(out BuckleComponent buckle) ||
                buckle.BuckledTo == null ||
                !buckle.BucklePosition.HasValue)
            {
                return;
            }

            var bucklePosition = buckle.BucklePosition.Value;

            if (moveEvent.NewPosition.InRange(_mapManager, bucklePosition, 0.2f))
            {
                return;
            }

            buckle.TryUnbuckle(buckle.Owner, true);
        }

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(BuckleComponent));

            UpdatesAfter.Add(typeof(InteractionSystem));
            UpdatesAfter.Add(typeof(InputSystem));

            SubscribeLocalEvent<MoveEvent>(MoveEvent);
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (!entity.TryGetComponent(out BuckleComponent buckle))
                {
                    continue;
                }

                buckle.Update();
            }
        }
    }
}
