#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Damage
{
    [UsedImplicitly]
    public class DamageVisualizer : AppearanceVisualizer
    {
        /// <summary>
        ///     Damage thresholds mapped to their state
        /// </summary>
        private readonly SortedDictionary<int, DamageVisualizerState> _lowestToHighestStates = new();

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("states", out var states))
            {
                var stateMap = (YamlSequenceNode) states;

                foreach (var stateNode in stateMap)
                {
                    var mapping = (YamlMappingNode) stateNode;
                    var reader = YamlObjectSerializer.NewReader(mapping, typeof(DamageVisualizerState));
                    var state = (DamageVisualizerState) reader.NodeToType(typeof(DamageVisualizerState), mapping);

                    _lowestToHighestStates.Add(state.Damage, state);
                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            if (!component.TryGetData(DamageVisualizerData.TotalDamage, out int damage))
            {
                return;
            }

            if (!TryGetState(damage, out var state))
            {
                return;
            }

            if (state.State == null)
            {
                return;
            }

            if (state.Sprite != null)
            {
                sprite.LayerSetTexture(state.Layer, state.Sprite);
            }

            sprite.LayerSetState(state.Layer, state.State);
        }

        private DamageVisualizerState? GetState(int damage)
        {
            foreach (var (threshold, state) in _lowestToHighestStates)
            {
                if (damage >= threshold)
                {
                    return state;
                }
            }

            return null;
        }

        private bool TryGetState(int damage, [NotNullWhen(true)] out DamageVisualizerState? state)
        {
            return (state = GetState(damage)) != null;
        }
    }
}
