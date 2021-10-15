using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Damage
{
    /// <summary>
    ///     A simple visualizer for any entity with a DamageableComponent
    ///     to display the status of how damaged it is.
    ///
    ///     Can either be an overlay for an entity, or target multiple
    ///     layers on the same entity.
    ///
    ///     This can be disabled dynamically by passing into SetData,
    ///     key DamageVisualizerKeys.Disabled, value bool
    ///     (DamageVisualizerKeys lives in Content.Shared.Damage)
    /// </summary>
    public class DamageVisualizer : AppearanceVisualizer
    {
        /// <summary>
        ///     Damage thresholds between damage state changes.
        ///
        ///     If there are any negative thresholds, or there is
        ///     less than one threshold, the visualizer is marked
        ///     as invalid.
        /// </summary>
        /// <remarks>
        ///     A 'zeroth' threshold is automatically added,
        ///     and this list is automatically sorted for
        ///     efficiency beforehand. As such, the zeroth
        ///     threshold is not required - and negative
        ///     thresholds are automatically caught as
        ///     invalid. The zeroth threshold automatically
        ///     sets all layers to invisible, so a sprite
        ///     isn't required for it.
        /// </remarks>
        [DataField("thresholds", required: true)]
        private List<int> _thresholds = new();

        /// <summary>
        ///     Layers to target, by layerMapKey.
        ///     If a target layer map key is invalid
        ///     (in essence, undefined), then the target
        ///     layer is removed from the list for efficiency.
        ///
        ///     If no layers are valid, then the visualizer
        ///     is marked as invalid.
        ///
        ///     If this is not defined, however, the visualizer
        ///     instead adds an overlay to the sprite.
        /// </summary>
        /// <remarks>
        ///     Layers can be disabled here by passing
        ///     the layer's name as a key to SetData,
        ///     and passing in a bool set to either 'false'
        ///     to disable it, or 'true' to enable it.
        ///     Setting the layer as disabled will make it
        ///     completely invisible.
        /// </remarks>
        [DataField("targetLayers")]
        private readonly List<string>? _targetLayers;

        /// <summary>
        ///     The actual sprites for every damage group
        ///     that the entity should display visually.
        ///
        ///     This is keyed by a damage group identifier
        ///     (for example, Brute), and has a value
        ///     of a ResourcePath to a sprite's RSI.
        /// </summary>
        /// <remarks>
        ///     Any of the sprites here must have states
        ///     where the first part matches the target
        ///     layer state, the second part matching
        ///     to a target group, and the third part
        ///     matching one of the defined thresholds
        ///     above. If targetLayers is not defined,
        ///     then instead, the first part must be
        ///     'DamageOverlay'.
        ///
        ///     For example:
        ///     Chest_Brute_33 - targets a chest layer, brute group, 33 damage
        ///     DamageOverlay_Burn_25 - overlay, burn, 25 damage
        /// </remarks>
        [DataField("damageSprites", required: true)]
        private readonly Dictionary<string, string> _damageSprites = new();

        [DataField("overlay")]
        private readonly bool _overlay = true;

        // this is here to ensure that the last state of the
        // layer's visibility is cached (can't
        // get a layer's visibility otherwise)
        private Dictionary<string, bool> _layerToggles = new();
        // this is here to ensure that layers stay disabled
        private Dictionary<string, bool> _disabledLayers = new();

        private bool _valid = true;
        private bool _disabled = false;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);
            InitializeVisualizer(entity);
        }

        private void InitializeVisualizer(IEntity entity)
        {
            if (!entity.TryGetComponent<SpriteComponent>(out SpriteComponent? spriteComponent)
                || !entity.TryGetComponent<DamageableComponent>(out var damageComponent)
                || !entity.TryGetComponent<AppearanceComponent>(out var appearanceComponent))
                return;

            appearanceComponent.SetData(DamageVisualizerKeys.Disabled, false);

            if (_thresholds.Count < 1)
            {
                Logger.ErrorS("DamageVisualizer", $"Thresholds were invalid for entity {entity.Name}. Thresholds: {_thresholds}");
                _valid = false;
                return;
            }

            _thresholds.Add(0);
            _thresholds.Sort();

            if (_thresholds[0] != 0)
            {
                Logger.ErrorS("DamageVisualizer", $"Thresholds were invalid for entity {entity.Name}. Thresholds: {_thresholds}");
                _valid = false;
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            if (damageComponent.DamageContainerID != null)
            {
                if (prototypeManager.TryIndex<DamageContainerPrototype>(damageComponent.DamageContainerID, out var damageContainer))
                {
                    foreach (string damageType in _damageSprites.Keys)
                        if (!damageContainer.SupportedGroups.Contains(damageType))
                        {
                            _damageSprites.Remove(damageType);
                        }
                }
            }
            else // oh boy! time to enumerate through every single group!
            {
                var damagePrototypeIdList = prototypeManager.EnumeratePrototypes<DamageGroupPrototype>()
                    .Select((p, _) => p.ID)
                    .ToList();
                foreach (string damageType in _damageSprites.Keys)
                    if (!damagePrototypeIdList.Contains(damageType))
                        _damageSprites.Remove(damageType);
            }

            if (_damageSprites.Keys.Count == 0)
            {
                Logger.ErrorS("DamageVisualizer", $"Damage keys were invalid for entity {entity.Name}.");
                _valid = false;
                return;
            }

            if (_targetLayers != null && _targetLayers.Count > 0)
            {
                foreach (var layer in _targetLayers)
                {
                    if (!spriteComponent.LayerMapTryGet(layer, out int index))
                    {
                        _targetLayers.Remove(layer);
                        continue;
                    }

                    foreach (var (group, sprite) in _damageSprites)
                    {
                        if (spriteComponent.LayerGetState(index) == null)
                            continue;

                        string? layerState = spriteComponent.LayerGetState(index).ToString();
                        if (layerState == null)
                            continue;
                        int newLayer = spriteComponent.AddBlankLayer(index + 1);
                        spriteComponent.LayerMapSet($"{layer}{group}", newLayer);
                    }
                    appearanceComponent.SetData(layer, true);
                    _layerToggles.Add(layer, true);
                    _disabledLayers.Add(layer, false);
                }

                if (_targetLayers.Count == 0)
                {
                    Logger.ErrorS("DamageVisualizer", $"Target layers were invalid for entity {entity.Name}.");
                    _valid = false;
                    return;
                }
            }
            else
            {
                foreach (var (group, sprite) in _damageSprites)
                {
                    int newLayer = spriteComponent.AddBlankLayer();
                    spriteComponent.LayerMapSet($"DamageOverlay{group}", newLayer);
                }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            if (!_valid)
                return;

            if (component.TryGetData<bool>(DamageVisualizerKeys.Disabled, out var disabledStatus))
            {
                if (disabledStatus != _disabled)
                    _disabled = disabledStatus;

                if (_disabled)
                    return;
            }

            HandleDamage(component);
        }

        private void HandleDamage(AppearanceComponent component)
        {
            if (!component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
                return;

            if (_targetLayers != null)
            {
                foreach (string layer in _targetLayers)
                {
                    if (component.GetData<bool>(layer) != _disabledLayers[layer])
                    {
                        _layerToggles[layer] = component.GetData<bool>(layer);
                        foreach (string damageGroup in _damageSprites.Keys)
                            spriteComponent.LayerSetVisible($"{layer}{damageGroup}", _disabledLayers[layer]);
                    }
                }
            }

            if (!component.TryGetData<DamageSpecifier>(DamageVisualizerKeys.DamageSpecifier, out DamageSpecifier? specifier)
                || !component.Owner.TryGetComponent<DamageableComponent>(out var damageComponent))
                return;

            foreach (var (damageGroup, damage) in specifier.GetDamagePerGroup())
            {
                int threshold = 0;
                int thresholdIndex = _thresholds.BinarySearch(damage);

                if (thresholdIndex < 0)
                {
                    thresholdIndex = ~thresholdIndex;
                    threshold = _thresholds[thresholdIndex - 1];
                }
                else
                {
                    threshold = _thresholds[thresholdIndex];
                }

                if (_targetLayers != null)
                {
                    Logger.DebugS("DamageVisualizer", "Attempting to set target layers now.");
                    foreach (var layerMapName in _targetLayers)
                        if (_damageSprites.ContainsKey(damageGroup) && !_disabledLayers[layerMapName])
                        {
                            if (!spriteComponent.LayerMapTryGet($"{layerMapName}", out int originalSprite))
                                continue;

                            if (spriteComponent.LayerGetState(originalSprite) == null)
                                continue;

                            string? layerState = spriteComponent.LayerGetState(originalSprite).ToString();
                            if (layerState == null)
                                continue;

                            spriteComponent.LayerMapTryGet($"{layerMapName}{damageGroup}", out int spriteLayer);

                            if (threshold == 0 && _layerToggles[layerMapName])
                            {
                                spriteComponent.LayerSetVisible(spriteLayer, false);
                                _layerToggles[layerMapName] = false;
                            }
                            else
                            {
                                if (!_layerToggles[layerMapName])
                                {
                                    spriteComponent.LayerSetVisible(spriteLayer, true);
                                    _layerToggles[layerMapName] = true;
                                }
                                spriteComponent.LayerSetState(spriteLayer, $"{layerState}_{damageGroup}_{threshold}");
                            }
                        }
                }
                else
                {
                    Logger.DebugS("DamageVisualizer", "Attempting to set overlay now.");
                    if (_damageSprites.ContainsKey(damageGroup))
                    {
                        spriteComponent.LayerMapTryGet($"DamageOverlay{damageGroup}", out int spriteLayer);
                        if (spriteLayer + 1 < spriteComponent.AllLayers.Count())
                        {
                            // ensure that the overlay is always on top
                            Logger.DebugS("DamageVisualizer", "Attempting to re-order overlay to top.");
                            spriteComponent.RemoveLayer(spriteLayer);
                            spriteLayer = spriteComponent.AddBlankLayer();
                            spriteComponent.LayerMapSet($"DamageOverlay{damageGroup}", spriteLayer);
                        }

                        if (threshold == 0)
                        {
                            spriteComponent.LayerSetVisible(spriteLayer, false);
                        }
                        else
                        {
                            spriteComponent.LayerSetVisible(spriteLayer, true);
                            spriteComponent.LayerSetState(spriteLayer, $"DamageOverlay_{damageGroup}_{threshold}");
                        }
                    }
                }
            }
        }
    }
}
