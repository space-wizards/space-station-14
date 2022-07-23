using Content.Shared.Camera;
using Content.Shared.Gravity;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
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

        [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;

        private Dictionary<EntityUid, uint> _gridsToShake = new();

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

        public void ShakeGrid(EntityUid gridId, GravityComponent comp)
        {
            _gridsToShake[gridId] = ShakeTimes;

            SoundSystem.Play(comp.GravityShakeSound.GetSound(),
                Filter.BroadcastGrid(gridId), AudioParams.Default.WithVolume(-2f));
        }

        private void ShakeGrids()
        {
            // I have to copy this because C# doesn't allow changing collections while they're
            // getting enumerated.
            var gridsToShake = new Dictionary<EntityUid, uint>(_gridsToShake);
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

        private void ShakeGrid(EntityUid gridId)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} attached
                    || EntityManager.GetComponent<TransformComponent>(attached).GridUid != gridId
                    || !EntityManager.HasComponent<CameraRecoilComponent>(attached))
                {
                    continue;
                }

                var kick = new Vector2(_random.NextFloat(), _random.NextFloat()) * GravityKick;
                _sharedCameraRecoil.KickCamera(player.AttachedEntity.Value, kick);
            }
        }
    }
}
