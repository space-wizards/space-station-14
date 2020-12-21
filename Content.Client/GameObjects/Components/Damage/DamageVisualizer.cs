#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Damage
{
    [UsedImplicitly]
    public class DamageVisualizer : AppearanceVisualizer
    {
        /// <summary>
        ///     Damage thresholds mapped to the layer that they modify.
        ///     The states are checked until the first matches, in the order defined
        ///     in the YAML prototype.
        /// </summary>
        private readonly Dictionary<int, List<DamageVisualizerState>> _layerStates = new();

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
                    var state = reader.NodeToType<DamageVisualizerState>(mapping);
                    var layerStates = _layerStates.GetOrNew(state.Layer ?? -1);

                    layerStates.Add(state);
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

            int? totalDamage = null;
            if (component.TryGetData(DamageVisualizerData.TotalDamage, out int totalDamageTemp))
            {
                totalDamage = totalDamageTemp;
            }

            component.TryGetData(DamageVisualizerData.DamageClasses, out Dictionary<DamageClass, int>? damageClasses);

            component.TryGetData(DamageVisualizerData.DamageTypes, out Dictionary<DamageType, int>? damageTypes);

            foreach (var states in _layerStates.Values)
            {
                foreach (var state in states)
                {
                    if (state.Reached(totalDamage, damageClasses, damageTypes))
                    {
                        if (state.State == null)
                        {
                            break;
                        }

                        if (state.Sprite != null)
                        {
                            var path = SharedSpriteComponent.TextureRoot / state.Sprite;
                            var rsi = IoCManager.Resolve<IResourceCache>().GetResource<RSIResource>(path).RSI;

                            if (state.Layer == null)
                            {
                                sprite.BaseRSI = rsi;
                            }
                            else
                            {
                                sprite.LayerSetRSI(state.Layer.Value, rsi);
                            }
                        }

                        var layerKey = state.Layer ?? 0;

                        sprite.LayerMapReserveBlank(layerKey);
                        sprite.LayerSetState(layerKey, state.State);

                        break;
                    }
                }
            }
        }
    }
}
