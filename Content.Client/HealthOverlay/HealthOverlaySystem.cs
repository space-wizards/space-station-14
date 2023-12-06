using Content.Client.HealthOverlay.UI;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.HealthOverlay
{
    [UsedImplicitly]
    public sealed class HealthOverlaySystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPlayerManager _player = default!;

        private readonly Dictionary<EntityUid, HealthOverlayGui> _guis = new();
        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                foreach (var gui in _guis.Values)
                {
                    gui.SetVisibility(value);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var gui in _guis.Values)
            {
                gui.Dispose();
            }

            _guis.Clear();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
            {
                return;
            }

            if (_player.LocalEntity is not {} ent || Deleted(ent))
            {
                return;
            }

            var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);

            var query = EntityQueryEnumerator<MobStateComponent, DamageableComponent>();
            while (query.MoveNext(out var entity, out var mobState, out _))
            {
                if (_entities.GetComponent<TransformComponent>(ent).MapID != _entities.GetComponent<TransformComponent>(entity).MapID ||
                    !viewBox.Contains(_entities.GetComponent<TransformComponent>(entity).WorldPosition))
                {
                    if (_guis.TryGetValue(entity, out var oldGui))
                    {
                        _guis.Remove(entity);
                        oldGui.Dispose();
                    }

                    continue;
                }

                if (_guis.ContainsKey(entity))
                {
                    continue;
                }

                var gui = new HealthOverlayGui(entity);
                _guis.Add(entity, gui);
            }
        }
    }
}
