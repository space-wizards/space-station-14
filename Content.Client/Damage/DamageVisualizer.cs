using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;

// apologies in advance for all the != null checks,
// my IDE wouldn't stop complaining about these

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

        [Dependency] IPrototypeManager _prototypeManager = default!;
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
        private List<string>? _targetLayers;
        // This is where all the keys are stored after the
        // entity is initialized.
        private List<object> _targetLayerMapKeys = new();

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
        [DataField("damageSprites")]
        private readonly Dictionary<string, string>? _damageSprites;

        /// <summary>
        ///     Sets if you want sprites to overlay the
        ///     entity when damaged, or if you would
        ///     rather have each target layer's state
        ///     replaced by a different state
        ///     within its RSI.
        ///
        ///     This cannot be set to false if:
        ///     - There are no target layers
        ///     - There is no damage group
        /// </summary>
        [DataField("overlay")]
        private readonly bool _overlay = true;

        /// <summary>
        ///     A single damage group to target.
        ///     This should only be defined if
        ///     overlay is set to false.
        ///     If this is defined with damageSprites,
        ///     this will be ignored.
        /// </summary>
        /// <remarks>
        ///     This is here because otherwise,
        ///     you would need several permutations
        ///     of group sprites depending on
        ///     what kind of damage combination
        ///     you would want, on which threshold.
        /// </remarks>
        [DataField("damageGroup")]
        private readonly string? _damageGroup;

        private Dictionary<object, bool> _layerToggles = new();
        // this is here to ensure that layers stay disabled
        private Dictionary<object, bool> _disabledLayers = new();
        // this is here to ensure that targetted layers
        // have their original states within their layermaps
        // kept
        private Dictionary<object, string> _layerMapKeyStates = new();

        private Dictionary<string, int> _lastThresholdPerGroup = new();

        private bool _valid = true;
        private bool _disabled = false;

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            IoCManager.InjectDependencies(this);
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

            if (!_overlay && _targetLayers == null)
            {
                Logger.ErrorS("DamageVisualizer", $"Disabled overlay without target layers on {entity.Name}.");
                _valid = false;
                return;
            }

            if (!_overlay && _damageSprites != null)
            {
                Logger.ErrorS("DamageVisualizer", $"Disabled overlay with defined damage overlay sprites on {entity.Name}.");
                _valid = false;
                return;
            }

            if (_overlay && _damageSprites == null)
            {
                Logger.ErrorS("DamageVisualizer", $"Enabled overlay without defined damage overlay sprites on {entity.Name}.");
                _valid = false;
                return;
            }

            if (_damageSprites != null && _damageGroup != null)
            {
                Logger.WarningS("DamageVisualizer", $"Damage overlay sprites and damage group are both defined on {entity.Name}.");
            }

            _thresholds.Add(0);
            _thresholds.Sort();

            if (_thresholds[0] != 0)
            {
                Logger.ErrorS("DamageVisualizer", $"Thresholds were invalid for entity {entity.Name}. Thresholds: {_thresholds}");
                _valid = false;
                return;
            }

            if (damageComponent.DamageContainerID != null)
            {
                if (_prototypeManager.TryIndex<DamageContainerPrototype>(damageComponent.DamageContainerID, out var damageContainer))
                {
                    if (_damageSprites != null)
                    {
                        foreach (string damageType in _damageSprites.Keys)
                        {
                            if (!damageContainer.SupportedGroups.Contains(damageType))
                            {
                                _damageSprites.Remove(damageType);
                            }
                            _lastThresholdPerGroup.Add(damageType, 0);
                        }
                    }
                    else if (_damageGroup != null)
                    {
                        if (!damageContainer.SupportedGroups.Contains(_damageGroup))
                        {
                            Logger.ErrorS("DamageVisualizer", $"Damage keys were invalid for entity {entity.Name}.");
                            _valid = false;
                            return;
                        }

                        _lastThresholdPerGroup.Add(_damageGroup, 0);
                    }
                }
            }
            else // oh boy! time to enumerate through every single group!
            {
                var damagePrototypeIdList = _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>()
                    .Select((p, _) => p.ID)
                    .ToList();
                if (_damageSprites != null)
                    foreach (string damageType in _damageSprites.Keys)
                    {
                        if (!damagePrototypeIdList.Contains(damageType))
                            _damageSprites.Remove(damageType);
                        _lastThresholdPerGroup.Add(damageType, 0);
                    }
                else if (_damageGroup != null)
                {
                    if (!damagePrototypeIdList.Contains(_damageGroup))
                    {
                        Logger.ErrorS("DamageVisualizer", $"Damage keys were invalid for entity {entity.Name}.");
                        _valid = false;
                        return;
                    }

                    _lastThresholdPerGroup.Add(_damageGroup, 0);
                }
            }

            if (_damageSprites != null)
                if (_damageSprites.Keys.Count == 0)
                {
                    Logger.ErrorS("DamageVisualizer", $"Damage keys were invalid for entity {entity.Name}.");
                    _valid = false;
                    return;
                }

            if (_targetLayers != null && _targetLayers.Count > 0)
            {
                foreach (var keyString in _targetLayers)
                {
                    object key;
                    if (IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(keyString, out var @enum))
                    {
                        key = @enum;
                    }
                    else
                    {
                        key = keyString;
                    }

                    if (!spriteComponent.LayerMapTryGet(key, out int index)
                        || spriteComponent.LayerGetState(index).ToString() == null)
                        continue;

                    _targetLayerMapKeys.Add(key);
                };

                if (_targetLayerMapKeys.Count == 0)
                {
                    Logger.ErrorS("DamageVisualizer", $"Target layers were invalid for entity {entity.Name}.");
                    _valid = false;
                    return;
                }

                foreach (object layer in _targetLayerMapKeys)
                {
                    int layerCount = spriteComponent.AllLayers.Count();
                    int index = spriteComponent.LayerMapGet(layer);
                    string layerState = spriteComponent.LayerGetState(index)!.ToString()!;

                    if (index + 1 != layerCount)
                    {
                        index = index + 1;
                    }

                    _layerMapKeyStates.Add(layer, layerState);

                    if (_overlay && _damageSprites != null)
                    {
                        foreach (var (group, sprite) in _damageSprites)
                        {
                            int newLayer = spriteComponent.AddBlankLayer(index);
                            spriteComponent.LayerMapSet($"{layer}{group}", newLayer);
                            spriteComponent.LayerSetVisible(newLayer, false);
                        }
                        _layerToggles.Add(layer, false);
                        _disabledLayers.Add(layer, false);
                    }
                }
            }
            else
            {
                if (_damageSprites != null)
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

            if (_targetLayers != null && _damageSprites != null)
            {
                foreach (object layer in _targetLayerMapKeys)
                {
                    bool? layerStatus = null;
                    switch (layer)
                    {
                        case Enum layerEnum:
                            if (component.TryGetData<bool>(layerEnum, out var layerStateEnum))
                                layerStatus = layerStateEnum;
                            break;
                        case string layerString:
                            if (component.TryGetData<bool>(layerString, out var layerStateString))
                                layerStatus = layerStateString;
                            break;
                    }

                    if (layerStatus == null)
                        continue;

                    if (_disabledLayers[layer] != (bool) layerStatus)
                    {
                        _disabledLayers[layer] = (bool) layerStatus;
                        foreach (string damageGroup in _damageSprites.Keys)
                            spriteComponent.LayerSetVisible($"{layer}{damageGroup}", _disabledLayers[layer]);
                    }
                }
            }

            if (!component.TryGetData<DamageSpecifier>(DamageVisualizerKeys.DamageSpecifierDelta, out DamageSpecifier? delta)
                || !component.Owner.TryGetComponent<DamageableComponent>(out var damageComponent))
                return;

            foreach (var (damageGroup, _) in delta.GetDamagePerGroup())
            {
                if (!_overlay && damageGroup != _damageGroup)
                    continue;

                int threshold = 0;
                if (!_prototypeManager.TryIndex<DamageGroupPrototype>(damageGroup, out var damageGroupPrototype)
                    || !damageComponent.Damage.TryGetDamageInGroup(damageGroupPrototype, out int damageTotal))
                    continue;

                int thresholdIndex = _thresholds.BinarySearch(damageTotal);

                if (thresholdIndex < 0)
                {
                    thresholdIndex = ~thresholdIndex;
                    threshold = _thresholds[thresholdIndex - 1];
                }
                else
                {
                    threshold = _thresholds[thresholdIndex];
                }

                if (!_lastThresholdPerGroup.TryGetValue(damageGroup, out int lastThreshold)
                    || threshold == lastThreshold)
                    continue;

                _lastThresholdPerGroup[damageGroup] = threshold;

                if (_targetLayers != null)
                {
                    Logger.DebugS("DamageVisualizer", "Attempting to set target layers now.");
                    foreach (var layerMapKey in _targetLayerMapKeys)
                        if (_overlay && _damageSprites != null)
                        {
                            if (_damageSprites.ContainsKey(damageGroup) && !_disabledLayers[layerMapKey])
                            {
                                string layerState = _layerMapKeyStates[layerMapKey];
                                spriteComponent.LayerMapTryGet($"{layerMapKey}{damageGroup}", out int spriteLayer);

                                if (threshold == 0 && spriteComponent[spriteLayer].Visible)
                                {
                                    spriteComponent.LayerSetVisible(spriteLayer, false);
                                }
                                else
                                {
                                    if (!spriteComponent[spriteLayer].Visible)
                                    {
                                        spriteComponent.LayerSetVisible(spriteLayer, true);
                                    }
                                    spriteComponent.LayerSetState(spriteLayer, $"{layerState}_{damageGroup}_{threshold}");
                                }
                            }
                        }
                        else
                        {
                            string layerState = _layerMapKeyStates[layerMapKey];
                            spriteComponent.LayerMapTryGet(layerMapKey, out int spriteLayer);

                            if (threshold == 0)
                            {
                                spriteComponent.LayerSetState(spriteLayer, layerState);
                            }
                            else
                            {
                                spriteComponent.LayerSetState(spriteLayer, $"{layerState}_{damageGroup}_{threshold}");
                            }
                        }
                }
                else
                {
                    Logger.DebugS("DamageVisualizer", "Attempting to set overlay now.");
                    if (_damageSprites != null)
                    {
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
}
