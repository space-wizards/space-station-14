using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Mobs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using DrawDepth = Content.Shared.GameObjects.DrawDepth;

namespace Content.Client.GameObjects.Components.Mobs
{
    [UsedImplicitly]
    public sealed class DamageStateVisualizer : AppearanceVisualizer
    {
        private DamageStateVisualData _data = DamageStateVisualData.Normal;
        private Dictionary<DamageStateVisualData, string> _stateMap = new Dictionary<DamageStateVisualData,string>();
        private int? _originalDrawDepth = null;

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
            if (node.TryGetNode("normal", out var normal))
            {
                _stateMap.Add(DamageStateVisualData.Normal, normal.AsString());
            }

            if (node.TryGetNode("crit", out var crit))
            {
                _stateMap.Add(DamageStateVisualData.Crit, crit.AsString());
            }

            if (node.TryGetNode("dead", out var dead))
            {
                _stateMap.Add(DamageStateVisualData.Dead, dead.AsString());
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(DamageStateVisuals.State, out DamageStateVisualData data))
            {
                return;
            }

            if (_data == data)
            {
                return;
            }

            _data = data;

            if (_stateMap.TryGetValue(_data, out var state))
            {
                sprite.LayerSetState(DamageStateVisualLayers.Base, state);
            }

            // So they don't draw over mobs anymore
            if (_data == DamageStateVisualData.Dead)
            {
                _originalDrawDepth = sprite.DrawDepth;
                sprite.DrawDepth = (int) DrawDepth.FloorObjects;
            }
            else if (_originalDrawDepth != null)
            {
                sprite.DrawDepth = _originalDrawDepth.Value;
                _originalDrawDepth = null;
            }
        }
    }

    public enum DamageStateVisualLayers
    {
        Base
    }
}
