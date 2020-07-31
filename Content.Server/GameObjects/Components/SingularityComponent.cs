using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class SingularityComponent : Component, ICollideBehavior
    {
        public override string Name => "Singularity";

        private ICollidableComponent _collidableComponent;

        private CancellationToken token = new CancellationToken();

        private Random rand = new Random();

        private float _range;

        private EntityManager _entityManager;

        private SingularityController _singularityController;

        public override void Initialize()
        {
            base.Initialize();

            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
            _collidableComponent.Hard = false;
            _singularityController = _collidableComponent.EnsureController<SingularityController>();
            _singularityController.ControlledComponent = _collidableComponent;
            _entityManager = IoCManager.Resolve<EntityManager>();

            _collidableComponent = Owner.GetComponent<ICollidableComponent>();
            _collidableComponent.Hard = false;

            _singularityController = _collidableComponent.EnsureController<SingularityController>();
            _singularityController.ControlledComponent = _collidableComponent;

        }

        public void Update()
        {
            _singularityController.Push(new Vector2((rand.Next()), rand.Next()).Normalized, 5f);

            foreach (var entity in _entityManager.GetEntitiesInRange(Owner.Transform.GridPosition, _range))
            {

            }
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            entity.Delete();
        }

    }
}
