using Content.Client.HealthOverlay.UI;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.HealthOverlay
{
    public sealed class HealthOverlaySystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private readonly Dictionary<EntityUid, HealthOverlayGui> _guis = new();
        private EntityUid? _attachedEntity;
        private bool _enabled;
        private List<string> _damageContainers = new();

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

        public void ClearDamageContainers()
        {
            _damageContainers.Clear();
        }

        public void AddDamageContainers(IEnumerable<string> damageContainerIds)
        {
            _damageContainers.AddRange(damageContainerIds);
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var gui in _guis.Values)
            {
                gui.Dispose();
            }

            _guis.Clear();
            _attachedEntity = default;
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage message)
        {
            _attachedEntity = message.AttachedEntity;
        }

        public override void FrameUpdate(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
            {
                return;
            }

            if (_attachedEntity is not {} ent || Deleted(ent))
            {
                return;
            }

            var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);

            foreach (var (mobState, damageable) in EntityManager.EntityQuery<MobStateComponent, DamageableComponent>())
            {
                var entity = mobState.Owner;

                if (_entities.GetComponent<TransformComponent>(ent).MapID != _entities.GetComponent<TransformComponent>(entity).MapID ||
                    !viewBox.Contains(_entities.GetComponent<TransformComponent>(entity).WorldPosition) ||
                    damageable.DamageContainerID == null ||
                    !_damageContainers.Contains(damageable.DamageContainerID))
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
