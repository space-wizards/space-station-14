using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.Components.Sound;
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

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class SingularityComponent : Component, ICollideBehavior
    {
        public override string Name => "Singularity";

        public int Energy = 100;
        public int Level = 1;

        private Random rand = new Random();

        private SingularityController _singularityController;
        private IEntityManager _entityManager;

        private ICollidableComponent _collidableComponent;

        private IMapManager _mapManager;

        private SpriteComponent _spriteComponent;


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

        public void Update()
        {
            Energy--;

            _singularityController.Push(new Vector2((rand.Next(-10, 10)), rand.Next(-10, 10)).Normalized, 5f);

            foreach (var entity in _entityManager.GetEntitiesInRange(Owner.Transform.GridPosition, 15))
            {
                if (entity.TryGetComponent<RadiationPanel>(out var radPanel))
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
            float radius = 0.5f;

            if (Energy == 0)
            {
                //collapse
            }

            if (Energy < 200)
            {
                Level = 1;
            }

            if (Energy >= 200)
            {
                Level = 2;
                radius = 1.5f;
            }

            if (Energy >= 300)
            {
                Level = 3;
                radius = 2.5f;
            }

            if (Energy >= 600)
            {
                Level = 4;
                radius = 3.5f;
            }

            if (Energy >= 1000)
            {
                Level = 5;
                radius = 4.5f;
            }

            if (Energy >= 1500)
            {
                Level = 6;
                radius = 5.5f;
            }

            if (Level != prevLevel)
            {
                _spriteComponent.LayerSetRSI(0, "Effects/Singularity/singularity_" + Level.ToString() + ".rsi");
                _spriteComponent.LayerSetState(0, "singularity_" + Level.ToString());

                (_collidableComponent.PhysicsShapes[0] as PhysShapeCircle).Radius = radius;
            }

        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (!ContainerHelpers.IsInContainer(entity))
            {
                Energy++;
                entity.Delete();
            }
        }
    }
}
