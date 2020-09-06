using Content.Shared.Physics;
using Content.Server.Atmos.Reactions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    class GasVaporComponent : Component, ICollideBehavior, IGasMixtureHolder
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Name => "GasVapor";

        [ViewVariables] public GasMixture Air { get; set; }

        private bool _running;
        private Vector2 _direction;
        private float _velocity;
        private float _disspateTimer = 0;
        private float _dissipationInterval;
        private Gas _gas;
        private float _gasVolume;
        private float _gasTemperature;
        private float _gasAmount;

        public override void Initialize()
        {
            base.Initialize();
            Air = new GasMixture(_gasVolume){Temperature = _gasTemperature};
            Air.SetMoles(_gas,_gasAmount);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _dissipationInterval, "dissipationInterval", 1);
            serializer.DataField(ref _gas, "gas", Gas.WaterVapor);
            serializer.DataField(ref _gasVolume, "gasVolume", 200);
            serializer.DataField(ref _gasTemperature, "gasTemperature", Atmospherics.T20C);
            serializer.DataField(ref _gasAmount, "gasAmount", 20);
        }

        public void StartMove(Vector2 dir, float velocity)
        {
            _running = true;
            _direction = dir;
            _velocity = velocity;

            if (Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                var controller = collidable.EnsureController<GasVaporController>();
                controller.Move(_direction, _velocity);
            }
        }

        public void Update(float frameTime)
        {
            if (!_running)
                return;

            if (Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                var worldBounds = collidable.WorldAABB;
                var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);

                var tiles = mapGrid.GetTilesIntersecting(worldBounds);

                foreach (var tile in tiles)
                {
                    var pos = tile.GridIndices.ToEntityCoordinates(_mapManager, tile.GridIndex);
                    var atmos = pos.GetTileAtmosphere(_entityManager);

                    if (atmos?.Air == null)
                    {
                        return;
                    }

                    if (atmos.Air.React(this) != ReactionResult.NoReaction)
                    {
                        Owner.Delete();
                    }
                }
            }

            _disspateTimer += frameTime;
            if (_disspateTimer > _dissipationInterval)
            {
                Air.SetMoles(_gas, Air.TotalMoles/2 );
            }

            if (Air.TotalMoles < 1)
            {
                Owner.Delete();
            }
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            // Check for collision with a impassable object (e.g. wall) and stop
            if (collidedWith.TryGetComponent(out ICollidableComponent collidable) &&
                (collidable.CollisionLayer & (int) CollisionGroup.Impassable) != 0 &&
                collidable.Hard &&
                Owner.TryGetComponent(out ICollidableComponent coll))
            {
                var controller = coll.EnsureController<GasVaporController>();
                controller.Stop();
                Owner.Delete();
            }
        }
    }
}
