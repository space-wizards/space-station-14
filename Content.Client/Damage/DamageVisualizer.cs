using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

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
    ///
    ///     Damage layers, if targetting layers, can also be dynamically
    ///     disabled if needed by passing into SetData, the name/enum
    ///     of the sprite layer, and then passing in a bool value
    ///     (true to enable, false to disable).
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
        ///     of a DamageVisualizerSprite (see below)
        /// </summary>
        [DataField("damageOverlaySprites")]
        private readonly Dictionary<string, DamageVisualizerSprite>? _damageOverlaySprites;

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

        // this is here to ensure that layers stay disabled
        private Dictionary<object, bool> _disabledLayers = new();
        // this is here to ensure that targetted layers
        // have their original states within their layermaps
        // kept
        private Dictionary<object, string> _layerMapKeyStates = new();

        private Dictionary<string, int> _lastThresholdPerGroup = new();
        private string _topMostLayerKey = default!;

        // deals with the edge case of human damage visuals not
        // being in color without making a Dict<Dict<Dict<Dict<Dict<Dict...
        [DataDefinition]
        internal class DamageVisualizerSprite
        {
            /// <summary>
            ///     The RSI path for the damage visualizer
            ///     group overlay.
            /// </summary>
            /// <remarks>
            ///     States in here will require one of two
            ///     forms:
            ///     - {base_state}_{group}_{threshold} if targetting
            ///       a static layer on a sprite (either as an
            ///       overlay or as a state change)
            ///     - DamageOverlay_{group_{threshold} if not
            ///       targetting a layer on a sprite.
            /// </remarks>
            [DataField("sprite", required: true)]
            public readonly string Sprite = default!;

            /// <summary>
            ///     The color of this sprite overlay.
            ///     Supports only hexadecimal format.
            /// </summary>
            [DataField("color")]
            public readonly string? Color;
        }

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

            if (!_overlay && _damageOverlaySprites != null)
            {
                Logger.ErrorS("DamageVisualizer", $"Disabled overlay with defined damage overlay sprites on {entity.Name}.");
                _valid = false;
                return;
            }

            if (_overlay && _damageOverlaySprites == null)
            {
                Logger.ErrorS("DamageVisualizer", $"Enabled overlay without defined damage overlay sprites on {entity.Name}.");
                _valid = false;
                return;
            }

            if (!_overlay && _damageGroup == null)
            {
                Logger.ErrorS("DamageVisualizer", $"Disabled overlay without defined damage group on {entity.Name}.");
                _valid = false;
                return;
            }

            if (_damageOverlaySprites != null && _damageGroup != null)
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
                    if (_damageOverlaySprites != null)
                    {
                        foreach (string damageType in _damageOverlaySprites.Keys)
                        {
                            if (!damageContainer.SupportedGroups.Contains(damageType))
                            {
                                _damageOverlaySprites.Remove(damageType);
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
                if (_damageOverlaySprites != null)
                    foreach (string damageType in _damageOverlaySprites.Keys)
                    {
                        if (!damagePrototypeIdList.Contains(damageType))
                            _damageOverlaySprites.Remove(damageType);
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

            if (_damageOverlaySprites != null)
                if (_damageOverlaySprites.Keys.Count == 0)
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

                    if (_overlay && _damageOverlaySprites != null)
                    {
                        foreach (var (group, sprite) in _damageOverlaySprites)
                        {
                            int newLayer = spriteComponent.AddLayer(
                                new SpriteSpecifier.Rsi(
                                    new ResourcePath(sprite.Sprite),
                                    $"{layerState}_{group}_{_thresholds[1]}"
                                ),
                                index);
                            spriteComponent.LayerMapSet($"{layer}{group}", newLayer);
                            if (sprite.Color != null)
                                spriteComponent.LayerSetColor(newLayer, Color.FromHex(sprite.Color));
                            spriteComponent.LayerSetVisible(newLayer, false);
                        }
                        _disabledLayers.Add(layer, false);
                    }
                }
            }
            else
            {
                if (_damageOverlaySprites != null)
                    foreach (var (group, sprite) in _damageOverlaySprites)
                    {
                        int newLayer = spriteComponent.AddLayer(
                            new SpriteSpecifier.Rsi(
                                new ResourcePath(sprite.Sprite),
                                $"DamageOverlay_{group}_{_thresholds[1]}"));
                        spriteComponent.LayerMapSet($"DamageOverlay{group}", newLayer);
                        if (sprite.Color != null)
                            spriteComponent.LayerSetColor(newLayer, Color.FromHex(sprite.Color));
                        spriteComponent.LayerSetVisible(newLayer, false);
                        _topMostLayerKey = $"DamageOverlay{group}";
                    }
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            if (!_valid)
                return;

            if (component.TryGetData<bool>(DamageVisualizerKeys.Disabled, out var disabledStatus))
                if (disabledStatus != _disabled)
                    _disabled = disabledStatus;

            if (_disabled)
                return;

            HandleDamage(component);
        }

        private void HandleDamage(AppearanceComponent component)
        {
            if (!component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent)
                || !component.Owner.TryGetComponent<DamageableComponent>(out var damageComponent))
                return;

            if (_targetLayers != null && _damageOverlaySprites != null)
                UpdateDisabledLayers(spriteComponent, component);

            if (_overlay && _damageOverlaySprites != null && _targetLayers == null)
                CheckOverlayOrdering(spriteComponent);

            if (component.TryGetData<bool>(DamageVisualizerKeys.ForceUpdate, out bool update)
                && update)
            {
                ForceUpdateLayers(damageComponent, spriteComponent);
                return;
            }

            if (component.TryGetData<List<string>>(DamageVisualizerKeys.DamageUpdateGroups, out List<string>? delta))
                UpdateDamageVisuals(delta, damageComponent, spriteComponent);
        }

        private void UpdateDisabledLayers(SpriteComponent spriteComponent, AppearanceComponent component)
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
                    foreach (string damageGroup in _damageOverlaySprites!.Keys)
                        spriteComponent.LayerSetVisible($"{layer}{damageGroup}", _disabledLayers[layer]);
                }
            }
        }

        private void CheckOverlayOrdering(SpriteComponent spriteComponent)
        {
            if (spriteComponent[_topMostLayerKey] != spriteComponent[spriteComponent.AllLayers.Count() - 1])
            {
                foreach (var (damageGroup, sprite) in _damageOverlaySprites!)
                {
                    Logger.DebugS("DamageVisualizer", "Attempting to re-order overlay to top.");
                    spriteComponent.LayerMapTryGet($"DamageOverlay{damageGroup}", out int spriteLayer);
                    bool visibility = spriteComponent[spriteLayer].Visible;
                    int threshold = _lastThresholdPerGroup[damageGroup];
                    spriteComponent.RemoveLayer(spriteLayer);
                    if (threshold == 0)
                        spriteLayer = spriteComponent.AddBlankLayer();
                    else
                        spriteLayer = spriteComponent.AddLayer(
                            new SpriteSpecifier.Rsi(
                                new ResourcePath(sprite.Sprite),
                                $"DamageOverlay_{damageGroup}_{threshold}"
                            ),
                            spriteLayer);
                    spriteComponent.LayerMapSet($"DamageOverlay{damageGroup}", spriteLayer);
                    spriteComponent.LayerSetVisible(spriteLayer, visibility);
                    // this is somewhat iffy since it constantly reallocates
                    _topMostLayerKey = $"DamageOverlay{damageGroup}";
                }
            }
        }

        private void UpdateDamageVisuals(List<string> delta, DamageableComponent damageComponent, SpriteComponent spriteComponent)
        {
            foreach (var damageGroup in delta)
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
                        UpdateTargetLayer(spriteComponent, layerMapKey, damageGroup, threshold);
                }
                else
                {
                    Logger.DebugS("DamageVisualizer", "Attempting to set overlay now.");
                    UpdateOverlay(spriteComponent, damageGroup, threshold);
                }
            }

        }

        private void ForceUpdateLayers(DamageableComponent damageComponent, SpriteComponent spriteComponent)
        {
            if (_damageOverlaySprites != null)
            {
                UpdateDamageVisuals(_damageOverlaySprites.Keys.ToList(), damageComponent, spriteComponent);
            }
            else if (_damageGroup != null)
            {
                UpdateDamageVisuals(new List<string>(){ _damageGroup }, damageComponent, spriteComponent);
            }
        }

        private void UpdateTargetLayer(SpriteComponent spriteComponent, object layerMapKey, string damageGroup, int threshold)
        {
            if (_overlay && _damageOverlaySprites != null)
            {
                if (_damageOverlaySprites.ContainsKey(damageGroup) && !_disabledLayers[layerMapKey])
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

        private void UpdateOverlay(SpriteComponent spriteComponent, string damageGroup, int threshold)
        {
            if (_damageOverlaySprites != null)
            {
                if (_damageOverlaySprites.ContainsKey(damageGroup))
                {
                    spriteComponent.LayerMapTryGet($"DamageOverlay{damageGroup}", out int spriteLayer);

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
