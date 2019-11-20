using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent]
    public class PuddleComponent : Component
    {
        // Current design: Something calls the SpillHelper.Spill, that will either
        // A) Add to an existing puddle at the location (normalised to tile-center) or
        // B) add a new one
        // From this every time a puddle is spilt on it will try and overflow to its neighbours if possible,
        // and also update its appearance based on volume level (opacity) and chemistry color
        // Small puddles will evaporate after a set delay

        // TODO: 'leaves fluidtracks', probably in a separate component for stuff like gibb chunks?;
        // TODO: Add stuff like slipping -> probably in a separate component (for stuff like bananas) and using BumpEntMsg

#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        // based on behaviour (e.g. someone being punched vs slashed with a sword would have different blood sprite)
        // to check for low volumes for evaporation or whatever

        // TODO: If the underlying tile becomes space then this should be deleted
        // it doesn't seem like there's an existing event for it.
        public override string Name => "Puddle";

        private CancellationTokenSource _evaporationToken;
        private int _evaporateThreshold; // How few <Solution Quantity> we can hold prior to self-destructing
        private float _evaporateTime;
        private string _spillSound;
        private DateTime _lastOverflow = DateTime.Now;
        private SpriteComponent _spriteComponent;

        public int MaxVolume
        {
            get => _contents.MaxVolume;
            set => _contents.MaxVolume = value;
        }

        [ViewVariables]
        public int CurrentVolume => _contents.CurrentVolume;

        // Volume at which the fluid will try to spill to adjacent components
        // Currently a random number, potentially change
        public int OverflowVolume => _overflowVolume;
        [ViewVariables]
        private int _overflowVolume;

        private SolutionComponent _contents;
        private int _spriteVariants;
        // Whether the underlying solution color should be used
        private bool _recolor;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _spillSound, "spill_sound", "/Audio/effects/Fluids/splat.ogg");
            serializer.DataField(ref _overflowVolume, "overflow_volume", 20);
            serializer.DataField(ref _evaporateTime, "evaporate_time", 600.0f);
            // Long-term probably have this based on the underlying reagents
            serializer.DataField(ref _evaporateThreshold, "evaporate_threshold", 2);
            serializer.DataField(ref _spriteVariants, "variants", 1);
            serializer.DataField(ref _recolor, "recolor", false);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out SolutionComponent solutionComponent))
            {
                _contents = solutionComponent;
            }
            else
            {
                _contents = Owner.AddComponent<SolutionComponent>();
                _contents.Initialize();
            }

            // Smaller than 1m^3 for now but realistically this shouldn't be hit
            MaxVolume = 1000;

            // Random sprite state set server-side so it's consistent across all clients
            _spriteComponent = Owner.GetComponent<SpriteComponent>();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var randomVariant = robustRandom.Next(0, _spriteVariants - 1);
            // TODO: This is ugly
            string[] splitRSI = _spriteComponent.BaseRSIPath.Split("/".ToCharArray());
            var baseName = splitRSI[splitRSI.Length - 1].Replace(".rsi", "");

            _spriteComponent.LayerSetState(0, $"{baseName}-{randomVariant}"); // TODO: Remove hardcode
            _spriteComponent.Rotation = Angle.FromDegrees(robustRandom.Next(0, 359));
            // UpdateAppearance should get called soon after this so shouldn't need to call Dirty() here
        }

        // Flow rate should probably be controlled globally so this is it for now
        internal bool TryAddSolution(Solution solution, bool sound = true, bool checkForEvaporate = true)
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

            UpdateStatus();
            CheckOverflow();
            if (checkForEvaporate)
            {
                CheckEvaporate();
            }

            UpdateAppearance();
            if (!sound)
            {
                return true;
            }

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play(_spillSound);
            return true;
        }

        internal Solution SplitSolution(int quantity)
        {
            var split = _contents.SplitSolution(quantity);
            CheckEvaporate();
            UpdateAppearance();
            return split;
        }

        public void CheckEvaporate()
        {
            if (CurrentVolume == 0)
            {
                Owner.Delete();
            }
        }

        private void UpdateStatus()
        {
            // If UpdateStatus is getting called again it means more fluid has been updated so let's just wait
            _evaporationToken?.Cancel();

            if (CurrentVolume > _evaporateThreshold)
            {
                return;
            }

            _evaporationToken = new CancellationTokenSource();

            // KYS to evaporate
            Timer.Spawn(TimeSpan.FromSeconds(_evaporateTime), CheckEvaporate, _evaporationToken.Token);
        }

        private void UpdateAppearance()
        {
            if (Owner.Deleted)
            {
                return;
            }
            // Opacity based on level of fullness to overflow
            // Hard-cap lower bound for visibility reasons
            var volumeScale = ((float)CurrentVolume / OverflowVolume) * 0.75f + 0.25f;
            var cappedScale = Math.Min(1.0f, volumeScale);
            // Color based on the underlying solutioncomponent
            Color newColor;
            if (_recolor)
            {
                newColor = new Color(
                    _contents.SubstanceColor.R,
                    _contents.SubstanceColor.G,
                    _contents.SubstanceColor.B,
                    cappedScale);
            }
            else
            {
                newColor = new Color(
                    _spriteComponent.Color.R,
                    _spriteComponent.Color.G,
                    _spriteComponent.Color.B,
                    cappedScale);
            }

            _spriteComponent.Color = newColor;

            _spriteComponent.Dirty();

        }

        /// <summary>
        /// Will overflow this entity to neighboring entities if required
        /// </summary>
        private void CheckOverflow()
        {
            if (CurrentVolume <= _overflowVolume)
            {
                return;
            }

            // Essentially:
            // Spill at least 1 solution to each neighbor (so most of the time each puddle is getting 1 max)
            // Find empty neighbors and prioritise those over neighbors with puddles already
            // If there are no empty neighbors then use the existing puddles
            // Divide the overflow amount between neighbors

            // TODO: Messy?

            // Setup
            var neighborGrids = GetNeighborTileGrids();
            _mapManager.TryGetGrid(Owner.Transform.GridID, out var grid);
            // If there's more neighbors to spill to then there are reagents to go around (coz integers)
            var overflowAmount = CurrentVolume - OverflowVolume;
            var remainingReagent = overflowAmount;

            // Essentially it'll prioritise tiles without a nearby puddle first,
            // and if there are none it'll spill to another nearby puddle
            var highPriorityPuddleCoords = new List<GridCoordinates>(8);
            var lowPriorityPuddles = new List<IEntity>(8);
            var neighborPuddles = new List<IEntity>(8);

            // Will overflow to each neighbor; if it already has a puddle entity intersecting then add to that
            // This is because Tiles aren't exactly entities

            foreach (var neighbor in neighborGrids)
            {
                // If we found an existing puddle on that tile then we don't need to spawn a new one
                var noSpawn = false;

                var neighborWorldPosition = grid.LocalToWorld(neighbor).Position; // Is this okay?
                foreach (var entity in _entityManager.GetEntitiesAt(neighborWorldPosition))
                {
                    // Don't overflow to walls
                    if (entity.TryGetComponent(out CollidableComponent collidableComponent) &&
                        collidableComponent.CollisionLayer == 1)
                    {
                        noSpawn = true;
                        break;
                    }

                    if (!entity.TryGetComponent(out PuddleComponent puddleComponent))
                    {
                        continue;
                    }

                    // If we've overflowed recently don't include it
                    noSpawn = true;
                    if ((DateTime.Now - puddleComponent._lastOverflow).TotalSeconds < 1)
                    {
                        break;
                    }

                    lowPriorityPuddles.Add(entity);
                    break;
                }

                if (noSpawn)
                {
                    continue;
                }

                // We'll just add the co-ordinates as we need to figure out how many puddles we need to spawn first
                highPriorityPuddleCoords.Add(neighbor);
            }

            foreach (var coord in highPriorityPuddleCoords)
            {
                if (remainingReagent <= 0)
                {
                    break;
                }
                remainingReagent--;
                neighborPuddles.Add(Owner.EntityManager.SpawnEntityAt(Owner.Prototype.ID, coord));
            }

            // If there's no free tiles to go to then just use an existing one
            if (neighborPuddles.Count == 0)
            {
                // Need to get how many neighbors we can spill to
                var puddleSpillLeft = lowPriorityPuddles.GetRange(0, Math.Min(remainingReagent, lowPriorityPuddles.Count));
                neighborPuddles.AddRange(puddleSpillLeft);
            }

            if (neighborPuddles.Count == 0)
            {
                return;
            }

            var spillAmount = neighborPuddles.Count / overflowAmount;

            SpillToNeighbours(neighborPuddles, spillAmount);
        }

        private void SpillToNeighbours(IEnumerable<IEntity> neighbors, int spillAmount)
        {
            foreach (var neighborPuddle in neighbors)
            {
                var solution = _contents.SplitSolution(spillAmount);

                neighborPuddle.GetComponent<PuddleComponent>().TryAddSolution(solution, false, false);
            }
        }

        private IEnumerable<GridCoordinates> GetNeighborTileGrids()
        {
            // That's when good neighbors
            // Become good friends
            Stack<GridCoordinates> neighbors = new Stack<GridCoordinates>();

            _mapManager.TryGetGrid(Owner.Transform.GridID, out var grid);
            var ownerTile = grid.GetTileRef(Owner.Transform.GridPosition);

            // Will currently also include diagonals
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var neighborIndices = new MapIndices(ownerTile.X + x, ownerTile.Y + y);
                    var neighborGridCoords = grid.GridTileToLocal(neighborIndices);
                    neighbors.Push(neighborGridCoords);
                }
            }

            return neighbors;
        }
    }
}
