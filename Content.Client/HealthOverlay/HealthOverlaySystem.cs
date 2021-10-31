using System.Collections.Generic;
using Content.Client.HealthOverlay.UI;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.HealthOverlay
{
    [UsedImplicitly]
    public class HealthOverlaySystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private readonly Dictionary<EntityUid, HealthOverlayGui> _guis = new();
        private IEntity? _attachedEntity;
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
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var gui in _guis.Values)
            {
                gui.Dispose();
            }

            _guis.Clear();
            _attachedEntity = null;
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

            if (_attachedEntity == null || _attachedEntity.Deleted)
            {
                return;
            }

            var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);

            foreach (var (mobState, _) in EntityManager.EntityQuery<IMobStateComponent, DamageableComponent>())
            {
                var entity = mobState.Owner;

                if (_attachedEntity.Transform.MapID != entity.Transform.MapID ||
                    !viewBox.Contains(entity.Transform.WorldPosition))
                {
                    if (_guis.TryGetValue(entity.Uid, out var oldGui))
                    {
                        oldGui.Dispose();
                    }

                    continue;
                }

                if (_guis.ContainsKey(entity.Uid))
                {
                    continue;
                }

                var gui = new HealthOverlayGui(entity);
                _guis.Add(entity.Uid, gui);
            }
        }
    }
}
