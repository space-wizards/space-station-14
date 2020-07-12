using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Gravity;
using Content.Server.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    public class GravitySystem: EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IRobustRandom _random;
#pragma warning restore 649

        private const float GravityKick = 100.0f;

        private const uint ShakeTimes = 10;

        private Dictionary<GridId, uint> _gridsToShake;

        private float internalTimer = 0.0f;

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery<GravityGeneratorComponent>();
            _gridsToShake = new Dictionary<GridId, uint>();
        }

        public override void Update(float frameTime)
        {
            internalTimer += frameTime;
            var gridsWithGravity = new List<GridId>();
            foreach (var entity in RelevantEntities)
            {
                var generator = entity.GetComponent<GravityGeneratorComponent>();
                if (generator.NeedsUpdate)
                {
                    generator.UpdateState();
                }
                if (generator.Status == GravityGeneratorStatus.On)
                {
                    gridsWithGravity.Add(entity.Transform.GridID);
                }
            }

            foreach (var grid in _mapManager.GetAllGrids())
            {
                if (grid.HasGravity && !gridsWithGravity.Contains(grid.Index))
                {
                    grid.HasGravity = false;
                    ScheduleGridToShake(grid.Index, ShakeTimes);
                } else if (!grid.HasGravity && gridsWithGravity.Contains(grid.Index))
                {
                    grid.HasGravity = true;
                    ScheduleGridToShake(grid.Index, ShakeTimes);
                }
            }

            if (internalTimer > 0.2f)
            {
                ShakeGrids();
                internalTimer = 0.0f;
            }
        }

        private void ScheduleGridToShake(GridId gridId, uint shakeTimes)
        {
            if (!_gridsToShake.Keys.Contains(gridId))
            {
                _gridsToShake.Add(gridId, shakeTimes);
            }
            else
            {
                _gridsToShake[gridId] = shakeTimes;
            }
            // Play the gravity sound
            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (player.AttachedEntity == null
                    || player.AttachedEntity.Transform.GridID != gridId) continue;
                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/alert.ogg", player.AttachedEntity);
            }
        }

        private void ShakeGrids()
        {
            // I have to copy this because C# doesn't allow changing collections while they're
            // getting enumerated.
            var gridsToShake = new Dictionary<GridId, uint>(_gridsToShake);
            foreach (var gridId in _gridsToShake.Keys)
            {
                if (_gridsToShake[gridId] == 0)
                {
                    gridsToShake.Remove(gridId);
                    continue;
                }
                ShakeGrid(gridId);
                gridsToShake[gridId] -= 1;
            }
            _gridsToShake = gridsToShake;
        }

        private void ShakeGrid(GridId gridId)
        {
            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (player.AttachedEntity == null
                    || player.AttachedEntity.Transform.GridID != gridId
                    || !player.AttachedEntity.TryGetComponent(out CameraRecoilComponent recoil))
                {
                    continue;
                }

                recoil.Kick(new Vector2(_random.NextFloat(), _random.NextFloat()) * GravityKick);
            }
        }
    }
}
