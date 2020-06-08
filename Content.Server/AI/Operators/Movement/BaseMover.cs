using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.AI.Operators.Movement
{
    public abstract class BaseMover : AiOperator
    {
        /// <summary>
        /// Invoked every time we move across a tile
        /// </summary>
        public event Action MovedATile;

        /// <summary>
        /// How close the pathfinder needs to get before returning a route
        /// Set at 1.42f just in case there's rounding and diagonally adjacent tiles aren't counted.
        ///
        /// </summary>
        public float PathfindingProximity { get; set; } = 1.42f;
        protected Queue<TileRef> Route = new Queue<TileRef>();
        /// <summary>
        ///  The final spot we're trying to get to
        /// </summary>
        protected GridCoordinates TargetGrid;
        /// <summary>
        /// As the pathfinder is tilebased we'll move to each tile's grid.
        /// </summary>
        protected GridCoordinates NextGrid;
        private const float TileTolerance = 0.2f;

        // Stuck checkers
        /// <summary>
        /// How long we're stuck in general before trying to unstuck
        /// </summary>
        private float _stuckTimerRemaining = 0.5f;
        private GridCoordinates _ourLastPosition;

        // Anti-stuck measures. See the AntiStuck() method for more details
        private bool _tryingAntiStuck;
        public bool IsStuck;
        private AntiStuckMethod _antiStuckMethod = AntiStuckMethod.Angle;
        private Angle _addedAngle = Angle.Zero;
        public event Action Stuck;
        private int _antiStuckAttempts = 0;

        private CancellationTokenSource _routeCancelToken;
        protected Job<Queue<TileRef>> RouteJob;
        private IMapManager _mapManager;
        private PathfindingSystem _pathfinder;
        private AiControllerComponent _controller;

        // Input
        protected IEntity Owner;

        protected void Setup(IEntity owner)
        {
            Owner = owner;
            _mapManager = IoCManager.Resolve<IMapManager>();
            _pathfinder = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<PathfindingSystem>();
            if (!Owner.TryGetComponent(out AiControllerComponent controllerComponent))
            {
                throw new InvalidOperationException();
            }

            _controller = controllerComponent;
        }

        protected void NextTile()
        {
            MovedATile?.Invoke();
        }

        /// <summary>
        /// Will move the AI towards the next position
        /// </summary>
        /// <returns>true if movement to be done</returns>
        protected bool TryMove()
        {
            // Use collidable just so we don't get stuck on corners as much
            // var targetDiff = NextGrid.Position - _ownerCollidable.WorldAABB.Center;
            var targetDiff = NextGrid.Position - Owner.Transform.GridPosition.Position;

            // Check distance
            if (targetDiff.Length < TileTolerance)
            {
                return false;
            }
            // Move towards it
            if (_controller == null)
            {
                return false;
            }
            _controller.VelocityDir = _addedAngle.RotateVec(targetDiff).Normalized;
            return true;

        }

        /// <summary>
        /// Will try and get around obstacles if stuck
        /// </summary>
        protected void AntiStuck(float frameTime)
        {
            // TODO: More work because these are sketchy af
            // TODO: Check if a wall was spawned in front of us and then immediately dump route if it was

            // First check if we're still in a stuck state from last frame
            if (IsStuck && !_tryingAntiStuck)
            {
                switch (_antiStuckMethod)
                {
                    case AntiStuckMethod.None:
                        break;
                    case AntiStuckMethod.Jiggle:
                        var randomRange = IoCManager.Resolve<IRobustRandom>().Next(0, 359);
                        var angle = Angle.FromDegrees(randomRange);
                        Owner.TryGetComponent(out AiControllerComponent mover);
                        mover.VelocityDir = angle.ToVec().Normalized;

                        break;
                    case AntiStuckMethod.PhaseThrough:
                        if (Owner.TryGetComponent(out CollidableComponent collidableComponent))
                        {
                            // TODO Fix this because they are yeeting themselves when they charge
                            // TODO: If something updates this this will fuck it
                            collidableComponent.CanCollide = false;

                            Timer.Spawn(100, () =>
                            {
                                if (!collidableComponent.CanCollide)
                                {
                                    collidableComponent.CanCollide = true;
                                }
                            });
                        }
                        break;
                    case AntiStuckMethod.Teleport:
                        Owner.Transform.DetachParent();
                        Owner.Transform.GridPosition = NextGrid;
                        break;
                    case AntiStuckMethod.ReRoute:
                        GetRoute();
                        break;
                    case AntiStuckMethod.Angle:
                        var random = IoCManager.Resolve<IRobustRandom>();
                        _addedAngle = new Angle(random.Next(-60, 60));
                        IsStuck = false;
                        Timer.Spawn(100, () =>
                        {
                            _addedAngle = Angle.Zero;
                        });
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            _stuckTimerRemaining -= frameTime;

            // Stuck check cooldown
            if (_stuckTimerRemaining > 0.0f)
            {
                return;
            }

            _tryingAntiStuck = false;
            _stuckTimerRemaining = 0.5f;

            // Are we actually stuck
            if ((_ourLastPosition.Position - Owner.Transform.GridPosition.Position).Length < TileTolerance)
            {
                _antiStuckAttempts++;

                // Maybe it's just 1 tile that's borked so try next 1?
                if (_antiStuckAttempts >= 2 && _antiStuckAttempts < 5 && Route.Count > 1)
                {
                    var nextTile = Route.Dequeue();
                    NextGrid = _mapManager.GetGrid(nextTile.GridIndex).GridTileToLocal(nextTile.GridIndices);
                    return;
                }

                if (_antiStuckAttempts >= 5 || Route.Count == 0)
                {
                    Logger.DebugS("ai", $"{Owner} is stuck at {Owner.Transform.GridPosition}, trying new route");
                    _antiStuckAttempts = 0;
                    IsStuck = false;
                    _ourLastPosition = Owner.Transform.GridPosition;
                    GetRoute();
                    return;
                }
                Stuck?.Invoke();
                IsStuck = true;
                return;
            }

            IsStuck = false;

            _ourLastPosition = Owner.Transform.GridPosition;
        }

        /// <summary>
        /// Tells us we don't need to keep moving and resets everything
        /// </summary>
        public void HaveArrived()
        {
            _routeCancelToken?.Cancel(); // oh thank god no more pathfinding
            Route.Clear();
            if (_controller == null) return;
            _controller.VelocityDir = Vector2.Zero;
        }

        protected void GetRoute()
        {
            _routeCancelToken?.Cancel();
            _routeCancelToken = new CancellationTokenSource();
            Route.Clear();

            int collisionMask;
            if (!Owner.TryGetComponent(out CollidableComponent collidableComponent))
            {
                collisionMask = 0;
            }
            else
            {
                collisionMask = collidableComponent.CollisionMask;
            }

            var startGrid = _mapManager.GetGrid(Owner.Transform.GridID).GetTileRef(Owner.Transform.GridPosition);
            var endGrid = _mapManager.GetGrid(TargetGrid.GridID).GetTileRef(TargetGrid);;
            // _routeCancelToken = new CancellationTokenSource();

            RouteJob = _pathfinder.RequestPath(new PathfindingArgs(
                Owner.Uid,
                collisionMask,
                startGrid,
                endGrid,
                PathfindingProximity
            ), _routeCancelToken.Token);
        }

        protected void ReceivedRoute()
        {
            Route = RouteJob.Result;
            RouteJob = null;

            if (Route == null)
            {
                Route = new Queue<TileRef>();
                // Couldn't find a route to target
                return;
            }

            // Because the entity may be half on 2 tiles we'll just cut out the first tile.
            // This may not be the best solution but sometimes if the AI is chasing for example it will
            // stutter backwards to the first tile again.
            Route.Dequeue();

            var nextTile = Route.Peek();
            NextGrid = _mapManager.GetGrid(nextTile.GridIndex).GridTileToLocal(nextTile.GridIndices);
        }

        public override Outcome Execute(float frameTime)
        {
            if (RouteJob != null && RouteJob.Status == JobStatus.Finished)
            {
                ReceivedRoute();
            }

            return !ActionBlockerSystem.CanMove(Owner) ? Outcome.Failed : Outcome.Continuing;
        }
    }

    public enum AntiStuckMethod
    {
        None,
        ReRoute,
        Jiggle, // Just pick a random direction for a bit and hope for the best
        Teleport, // The Half-Life 2 method
        PhaseThrough, // Just makes it non-collidable
        Angle, // Add a different angle for a bit
    }
}
