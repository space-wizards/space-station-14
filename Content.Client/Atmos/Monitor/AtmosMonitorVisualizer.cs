using System.Collections.Generic;
using Content.Shared.Atmos.Monitor;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Atmos.Monitor
{
    public sealed class AtmosMonitorVisualizer : AppearanceVisualizer
    {
        [Dependency] IEntityManager _entityManager = default!;
        [DataField("layerMap")]
        private string _layerMap { get; } = string.Empty;

        [DataField("alarmStates")]
        private readonly Dictionary<AtmosMonitorAlarmType, string> _alarmStates = new();

        [DataField("hideOnDepowered")]
        private readonly List<string>? _hideOnDepowered;

        // eh...
        [DataField("setOnDepowered")]
        private readonly Dictionary<string, string>? _setOnDepowered;

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            IoCManager.InjectDependencies(this);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            if (!_entityManager.TryGetComponent<SpriteComponent>(component.Owner, out var sprite))
                return;

            if (!sprite.LayerMapTryGet(_layerMap, out int layer))
                return;

            if (component.TryGetData<bool>("powered", out var powered))
            {
                if (_hideOnDepowered != null)
                    foreach (var visLayer in _hideOnDepowered)
                        if (sprite.LayerMapTryGet(visLayer, out int powerVisibilityLayer))
                            sprite.LayerSetVisible(powerVisibilityLayer, powered);

                if (_setOnDepowered != null && !powered)
                    foreach (var (setLayer, state) in _setOnDepowered)
                        if (sprite.LayerMapTryGet(setLayer, out int setStateLayer))
                            sprite.LayerSetState(setStateLayer, new RSI.StateId(state));
            }

            if (component.TryGetData<Vector2>("offset", out Vector2 offset))
            {
                sprite.Offset = offset;
            }

            if (component.TryGetData<AtmosMonitorAlarmType>("alarmType", out var alarmType)
                && powered)
                if (_alarmStates.TryGetValue(alarmType, out var state))
                    sprite.LayerSetState(layer, new RSI.StateId(state));
        }
    }
}
