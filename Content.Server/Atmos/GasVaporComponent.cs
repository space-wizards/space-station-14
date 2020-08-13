using Content.Server.GameObjects.Components.Fluids;
using Content.Server.Atmos;
using Content.Shared.Physics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Robust.Server.GameObjects;
using Content.Server.Atmos.Reactions;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Atmos;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.Atmos
{
    [RegisterComponent]
    class GasVaporComponent : Component, ICollideBehavior
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager = default!;
#pragma warning enable 649
        public override string Name => "GasVapor";

        //TODO: IDK if this is a good way to initilize contents
        [ViewVariables] public GasMixture contents;

        [ViewVariables] private GridAtmosphereComponent _gridAtmosphereComponent;
        //TODO: Add Whatever the gas scaler values is if there is one?
        //Does a gas dissapear when it recats?
        //[ViewVariables] private ReagentUnit _transferAmount;

        private bool _running;
        private Vector2 _direction;
        private float _velocity;


        public void Initialize(GridAtmosphereComponent gridAtmosphereComponent)
        {
            base.Initialize();
            _gridAtmosphereComponent = gridAtmosphereComponent;
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

        //TODO: Does the scaler amount of gas need to be exposed?
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(0.5));
        }

        //TODO: have moles/volume/pressure falloff over time exponentially
        public void Update()
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
                    var pos = tile.GridIndices.ToGridCoordinates(_mapManager, tile.GridIndex);
                    var atmos = AtmosHelpers.GetTileAtmosphere(pos);
                    //Here is were we check for reactions.
                }

            }

            //TODO: disspate once were out of Moles
            /*if (_contents.TotalMoles == 0)
            {
                // Delete this
                Owner.Delete();
            }*/
        }

        //TODO: TryReact
        /*
        internal bool TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }

            var result = _contents.TryAddSolution(solution);
            if (!result)
            {
                return false;
            }

            return true;
        }*/

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
            }
        }
    }
}
