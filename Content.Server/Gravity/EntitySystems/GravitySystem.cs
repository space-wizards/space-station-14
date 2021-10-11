using System.Collections.Generic;
using System.Linq;
using Content.Server.Camera;
using Content.Shared.Gravity;
using Content.Shared.Sound;
using JetBrains.Annotations;
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
    [UsedImplicitly]
    internal sealed class GravitySystem : SharedGravitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private const float GravityKick = 100.0f;

        private const uint ShakeTimes = 10;

        private Dictionary<GridId, uint> _gridsToShake = new();

        private float _internalTimer = 0.0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GravityComponent, ComponentInit>(HandleGravityInitialize);
            SubscribeLocalEvent<GravityGeneratorUpdateEvent>(HandleGeneratorUpdate);
        }

        private void HandleGeneratorUpdate(GravityGeneratorUpdateEvent ev)
        {
            if (ev.GridId == GridId.Invalid) return;

            var gravity = EntityManager.GetComponent<GravityComponent>(_mapManager.GetGrid(ev.GridId).GridEntityId);

            if (ev.Status == GravityGeneratorStatus.On)
            {
                EnableGravity(gravity);
            }
            else
            {
                DisableGravity(gravity);
            }
        }

        private void HandleGravityInitialize(EntityUid uid, GravityComponent component, ComponentInit args)
        {
            // Incase there's already a generator on the grid we'll just set it now.
            var gridId = component.Owner.Transform.GridID;
            GravityChangedMessage message;

            foreach (var generator in EntityManager.EntityQuery<GravityGeneratorComponent>(true))
            {
                if (generator.Owner.Transform.GridID == gridId && generator.Status == GravityGeneratorStatus.On)
                {
                    component.Enabled = true;
                    message = new GravityChangedMessage(gridId, true);
                    RaiseLocalEvent(message);
                    return;
                }
            }

            component.Enabled = false;
            message = new GravityChangedMessage(gridId, false);
            RaiseLocalEvent(message);
        }

        public override void Update(float frameTime)
        {
            // TODO: Pointless iteration, just make both of these event-based PLEASE
            foreach (var generator in EntityManager.EntityQuery<GravityGeneratorComponent>(true))
            {
                if (generator.NeedsUpdate)
                {
                    generator.UpdateState();
                }
            }

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

        private void EnableGravity(GravityComponent comp)
        {
            if (comp.Enabled) return;
            comp.Enabled = true;

            var gridId = comp.Owner.Transform.GridID;
            ScheduleGridToShake(gridId, ShakeTimes, comp);

            var message = new GravityChangedMessage(gridId, true);
            RaiseLocalEvent(message);
        }

        private void DisableGravity(GravityComponent comp)
        {
            if (!comp.Enabled) return;
            comp.Enabled = false;

            var gridId = comp.Owner.Transform.GridID;
            ScheduleGridToShake(gridId, ShakeTimes, comp);

            var message = new GravityChangedMessage(gridId, false);
            RaiseLocalEvent(message);
        }

        private void ScheduleGridToShake(GridId gridId, uint shakeTimes, GravityComponent comp)
        {
            if (!_gridsToShake.Keys.Contains(gridId))
            {
                _gridsToShake.Add(gridId, shakeTimes);
            }
            else
            {
                _gridsToShake[gridId] = shakeTimes;
            }

            SoundSystem.Play(Filter.BroadcastGrid(gridId), comp.GravityShakeSound.GetSound(), AudioParams.Default.WithVolume(-2f));
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
                    || !player.AttachedEntity.TryGetComponent(out CameraRecoilComponent? recoil))
                {
                    continue;
                }

                recoil.Kick(new Vector2(_random.NextFloat(), _random.NextFloat()) * GravityKick);
            }
        }
    }
}
