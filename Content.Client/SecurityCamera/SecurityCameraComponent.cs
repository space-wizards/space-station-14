using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.IoC;
using static Robust.Client.GameObjects.SpriteComponent;
using Content.Shared.Tag;

namespace Content.Client.SecurityCamera
{
    [RegisterComponent]
    public class ClientSecurityCameraComponent : Component
    {
        public override string Name => "ClientSecurityCamera";

        [ViewVariables]
        public bool Connected;

        protected override void Initialize()
        {
            base.Initialize();
            ChanceSpriteDirection();
        }

        private void ChanceSpriteDirection()
        {
            var dirs = 1;

            var position = Owner.Transform.Coordinates;

            var sprite = Owner.GetComponent<SpriteComponent>();


            if(!IoCManager.Resolve<IMapManager>().TryGetGrid(Owner.Transform.GridID,out var grid))return;

            if (MatchingEntity(grid.GetInDir(position, Direction.North)))
            {
                sprite.Offset = new Vector2(0f,0.5f);
                sprite.LayerSetDirOffset(0,SpriteComponent.DirectionOffset.None);
               return;
            }
            if (MatchingEntity(grid.GetInDir(position, Direction.South)))
            {
                sprite.Offset = new Vector2(0f,-0.5f);
                sprite.LayerSetDirOffset(0,SpriteComponent.DirectionOffset.Flip);
               return;
            }
            if (MatchingEntity(grid.GetInDir(position, Direction.East)))
            {
                sprite.Offset = new Vector2(0.5f,0f);
                sprite.LayerSetDirOffset(0,SpriteComponent.DirectionOffset.Clockwise);
               return;
            }
            if (MatchingEntity(grid.GetInDir(position, Direction.West)))
            {
                sprite.Offset = new Vector2(-0.5f,0f);
                sprite.LayerSetDirOffset(0,SpriteComponent.DirectionOffset.CounterClockwise);
               return;
            }

            Owner.QueueDelete();
        }
        protected bool MatchingEntity(IEnumerable<EntityUid> candidates)
        {
            foreach (var entity in candidates)
            {
                if (Owner.EntityManager.ComponentManager.TryGetComponent(entity, out TagComponent? other))
                {
                    if (other.HasTag("Wall"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
