using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Gravity;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Gravity;
using Content.Shared.GameObjects.EntitySystemMessages.Gravity;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GravitySystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const float GravityKick = 100.0f;

        private const uint ShakeTimes = 10;

        private Dictionary<GridId, uint> _gridsToShake = new();

        private float _internalTimer = 0.0f;

        public override void Update(float frameTime)
        {
            _internalTimer += frameTime;
            var gridsWithGravity = new List<GridId>();
            foreach (var generator in ComponentManager.EntityQuery<GravityGeneratorComponent>(true))
            {
                if (generator.NeedsUpdate)
                {
                    generator.UpdateState();
                }

                if (generator.Status == GravityGeneratorStatus.On)
                {
                    gridsWithGravity.Add(generator.Owner.Transform.GridID);
                }
            }

            foreach (var grid in _mapManager.GetAllGrids())
            {
                if (grid.HasGravity && !gridsWithGravity.Contains(grid.Index))
                {
                    DisableGravity(grid);
                }
                else if (!grid.HasGravity && gridsWithGravity.Contains(grid.Index))
                {
                    EnableGravity(grid);
                }
            }

            if (_internalTimer > 0.2f)
            {
                ShakeGrids();
                _internalTimer = 0.0f;
            }
        }

        private void EnableGravity(IMapGrid grid)
        {
            grid.HasGravity = true;
            ScheduleGridToShake(grid.Index, ShakeTimes);

            var message = new GravityChangedMessage(grid);

            RaiseLocalEvent(message);
        }

        private void DisableGravity(IMapGrid grid)
        {
            grid.HasGravity = false;
            ScheduleGridToShake(grid.Index, ShakeTimes);

            var message = new GravityChangedMessage(grid);

            RaiseLocalEvent(message);
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
                Get<AudioSystem>().PlayFromEntity("/Audio/Effects/alert.ogg", player.AttachedEntity);
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
