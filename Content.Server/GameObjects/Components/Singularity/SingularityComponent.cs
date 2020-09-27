using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class SingularityComponent : Component, ICollideBehavior
    {
        public override uint? NetID => ContentNetIDs.SINGULARITY;

        public override string Name => "Singularity";

        public int Energy = 100;
        public int Level = 1;

        private Random rand = new Random();

        private SingularityController _singularityController;
        private IEntityManager _entityManager;

        private ICollidableComponent _collidableComponent;

        private IMapManager _mapManager;

        private SpriteComponent _spriteComponent;

        private bool transition = true;

        private bool repelled = false;

        public override void Initialize()
        {
            base.Initialize();

            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
            _collidableComponent.Hard = false;

            _spriteComponent = Owner.GetComponent<SpriteComponent>();

            _singularityController = _collidableComponent.EnsureController<SingularityController>();
            _singularityController.ControlledComponent = _collidableComponent;

            _entityManager = IoCManager.Resolve<IEntityManager>();
            _mapManager = IoCManager.Resolve<IMapManager>();
        }

        protected override void Startup()
        {
            SendNetworkMessage(new SingularitySoundMessage(true));
            Timer.Spawn(5421, () => transition = false);
        }

        public void Update()
        {
            if (transition)
            {
                return;
            }

            switch (Level)
            {
                case 6:
                    Energy -= 20;
                    break;
                case 5:
                    Energy -= 15;
                    break;
                case 4:
                    Energy -= 10;
                    break;
                case 3:
                    Energy -= 5;
                    break;
                case 2:
                    Energy -= 2;
                    break;
                case 1:
                    Energy -= 1;
                    break;
            }

            Energy--;

            if (!repelled)
            {
                _singularityController.Push(new Vector2((rand.Next(-10, 10)), rand.Next(-10, 10)).Normalized, 5f);
            }

            foreach (var entity in _entityManager.GetEntitiesInRange(Owner.Transform.Coordinates, 15))
            {
                if (entity.TryGetComponent<RadiationPanelComponent>(out var radPanel))
                {
                    radPanel.Radiation += Level * 100;
                }
            }

            UpdateLevel();
        }

        public void TileUpdate()
        {
            IMapGrid mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);
            foreach (TileRef tile in mapGrid.GetTilesIntersecting(_collidableComponent.WorldAABB))
            {
                mapGrid.SetTile(tile.GridIndices, Tile.Empty);
            }
        }

        public void UpdateLevel()
        {
            int prevLevel = Level;
            float radius;

            if (Energy <= 0)
            {

                SendNetworkMessage(new SingularitySoundMessage(false));

                _singularityController.LinearVelocity = Vector2.Zero;
                transition = true;
                _spriteComponent.LayerSetVisible(0, false);

                Timer.Spawn(7500, () => Owner.Delete());

            }
            else
            {
                Level = Energy switch
                {
                    var n when n >= 1500 => 6,
                    var n when n >= 1000 => 5,
                    var n when n >= 600 => 4,
                    var n when n >= 300 => 3,
                    var n when n >= 200 => 2,
                    var n when n <  200 => 1
                };
            }

            radius = Level - 0.5f;

            if (Level != prevLevel)
            {
                _spriteComponent.LayerSetRSI(0, "Effects/Singularity/singularity_" + Level.ToString() + ".rsi");
                _spriteComponent.LayerSetState(0, "singularity_" + Level.ToString());

                (_collidableComponent.PhysicsShapes[0] as PhysShapeCircle).Radius = radius;
            }

        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (repelled)
            {
                return;
            }

            if (entity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var component) && component.Power >= 1)
            {
                return;
            }

            if (entity.HasComponent<ContainmentFieldComponent>())
            {
                repelled = true;
                Timer.Spawn(50, () => repelled = false);

                if (entity.Transform.WorldRotation.Degrees == -90f ||
                    entity.Transform.WorldRotation.Degrees == 90f)
                {
                    Vector2 normal = new Vector2(0.05f * Math.Sign(Owner.Transform.WorldPosition.X - entity.Transform.WorldPosition.X), 0);

                    if (normal == Vector2.Zero)
                    {
                        normal = new Vector2(0.05f, 0);
                    }

                    _singularityController.LinearVelocity = new Vector2(_singularityController.LinearVelocity.X * -1, _singularityController.LinearVelocity.Y);

                    while (_entityManager.GetEntitiesIntersecting(Owner).Contains(entity))
                    {
                        Owner.Transform.WorldPosition += normal;
                    }

                }

                else
                {
                    Vector2 normal = new Vector2(0, 0.05f * Math.Sign(Owner.Transform.WorldPosition.Y - entity.Transform.WorldPosition.Y));

                    if (normal == Vector2.Zero)
                    {
                        normal = new Vector2(0, 0.05f);
                    }

                    _singularityController.LinearVelocity = new Vector2(_singularityController.LinearVelocity.X, _singularityController.LinearVelocity.Y * -1);

                    while (_entityManager.GetEntitiesIntersecting(Owner).Contains(entity))
                    {
                        Owner.Transform.WorldPosition += normal;
                    }
                }

                return;
            }

            if (!ContainerHelpers.IsInContainer(entity))
            {
                Energy++;
                entity.Delete();
            }
        }
    }
}
