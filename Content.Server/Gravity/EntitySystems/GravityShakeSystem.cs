using System.Collections.Generic;
using Content.Shared.Camera;
using Content.Shared.Gravity;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Gravity.EntitySystems
{
    /// <summary>
    /// Handles the grid shake effect used by the gravity generator.
    /// </summary>
    public sealed class GravityShakeSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        [Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;

        private Dictionary<GridId, uint> _gridsToShake = new();

        private const float GravityKick = 100.0f;
        private const uint ShakeTimes = 10;

        private float _internalTimer;

        public override void Update(float frameTime)
        {
            if (_gridsToShake.Count > 0)
            {
                _internalTimer += frameTime;

                if (_internalTimer > 0.2f)
                {
                    // TODO: Could just have clients do this themselves via event and save bandwidth.
                    ShakeGrids();
                    _internalTimer -= 0.2f;
                }
            }
            else
            {
                _internalTimer = 0.0f;
            }
        }

        public void ShakeGrid(GridId gridId, GravityComponent comp)
        {
            _gridsToShake[gridId] = ShakeTimes;

            SoundSystem.Play(
                Filter.BroadcastGrid(gridId),
                comp.GravityShakeSound.GetSound(),
                AudioParams.Default.WithVolume(-2f));
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
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} attached
                    || EntityManager.GetComponent<TransformComponent>(attached).GridID != gridId
                    || !EntityManager.HasComponent<CameraRecoilComponent>(attached))
                {
                    continue;
                }

                var kick = new Vector2(_random.NextFloat(), _random.NextFloat()) * GravityKick;
                _cameraRecoil.KickCamera(player.AttachedEntity.Value, kick);
            }
        }
    }
}
