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
        [DataField("damageOverlayGroups")]
        private readonly Dictionary<string, DamageVisualizerSprite>? _damageOverlayGroups;

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

        // Avoids duplication. Set this if you have child classes
        // that share similar overlays, and you don't want to
        // create the same exact sprite a million times
        [DataField("damageDivisor")]
        private float _divisor = 1;

        // Set this to track all damage, instead of specific groups.
        // Useful if what you have is destroyable in any damage case.
        [DataField("trackAllDamage")]
        private readonly bool _trackAllDamage = false;
        // This is the overlay sprite used, if _trackAllDamage is
        // enabled (since you can't exactly define a group when
        // summing damage). Supports no complex per-group layering,
        // just an actually simple damage overlay.
        [DataField("damageOverlay")]
        private readonly DamageVisualizerSprite? _damageOverlay;
        // The last damage threshold tracked.
        private int _lastDamageThreshold = 0;

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
            VerifyVisualizerSetup(entity);
            if (_valid)
                InitializeVisualizer(entity);
        }

        private void VerifyVisualizerSetup(IEntity entity)
        {
            if (_thresholds.Count < 1)
            {
                Logger.ErrorS("DamageVisualizer", $"Thresholds were invalid for entity {entity.Name}. Thresholds: {_thresholds}");
                _valid = false;
                return;
            }

            if (_divisor == 0)
            {
                Logger.ErrorS("DamageVisualizer", $"Divisor for {entity.Name} is set to zero.");
                _valid = false;
                return;
            }

            if (_overlay)
            {
                if (_damageOverlayGroups == null && _damageOverlay == null)
                {
                    Logger.ErrorS("DamageVisualizer", $"Enabled overlay without defined damage overlay sprites on {entity.Name}.");
                    _valid = false;
                    return;
                }

                if (_trackAllDamage && _damageOverlay == null)
                {
                    Logger.ErrorS("DamageVisualizer", $"Enabled all damage tracking without a damage overlay sprite on {entity.Name}.");
                    _valid = false;
                    return;
                }

                if (!_trackAllDamage && _damageOverlay != null)
                {
                    Logger.WarningS("DamageVisualizer", $"Disabled all damage tracking with a damage overlay sprite on {entity.Name}.");
                    _valid = false;
                    return;
                }


                if (_trackAllDamage && _damageOverlayGroups != null)
                {
                    Logger.WarningS("DamageVisualizer", $"Enabled all damage tracking with damage overlay groups on {entity.Name}.");
                    _valid = false;
                    return;
                }
            }
            else if (!_overlay)
            {
                if (_targetLayers == null)
                {
                    Logger.ErrorS("DamageVisualizer", $"Disabled overlay without target layers on {entity.Name}.");
                    _valid = false;
                    return;
                }

                if (_damageOverlayGroups != null || _damageOverlay != null)
                {
                    Logger.ErrorS("DamageVisualizer", $"Disabled overlay with defined damage overlay sprites on {entity.Name}.");
                    _valid = false;
                    return;
                }

                if (_damageGroup == null)
                {
                    Logger.ErrorS("DamageVisualizer", $"Disabled overlay without defined damage group on {entity.Name}.");
                    _valid = false;
                    return;
                }
            }

            if (_damageOverlayGroups != null && _damageGroup != null)
            {
                Logger.WarningS("DamageVisualizer", $"Damage overlay sprites and damage group are both defined on {entity.Name}.");
            }

            if (_damageOverlay != null && _damageGroup != null)
            {
                Logger.WarningS("DamageVisualizer", $"Damage overlay sprites and damage group are both defined on {entity.Name}.");
            }

        }

        private void InitializeVisualizer(IEntity entity)
        {
            if (!entity.TryGetComponent<SpriteComponent>(out SpriteComponent? spriteComponent)
                || !entity.TryGetComponent<DamageableComponent>(out var damageComponent)
                || !entity.TryGetComponent<AppearanceComponent>(out var appearanceComponent))
                return;

            appearanceComponent.SetData(DamageVisualizerKeys.Disabled, false);

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
                    if (_damageOverlayGroups != null)
                    {
                        foreach (string damageType in _damageOverlayGroups.Keys)
                        {
                            if (!damageContainer.SupportedGroups.Contains(damageType))
                            {
                                _damageOverlayGroups.Remove(damageType);
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
                if (_damageOverlayGroups != null)
                    foreach (string damageType in _damageOverlayGroups.Keys)
                    {
                        if (!damagePrototypeIdList.Contains(damageType))
                            _damageOverlayGroups.Remove(damageType);
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

            if (_damageOverlayGroups != null)
                if (_damageOverlayGroups.Keys.Count == 0)
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

                    if (_overlay && _damageOverlayGroups != null)
                    {
                        foreach (var (group, sprite) in _damageOverlayGroups)
                        {
                            AddDamageLayerToSprite(spriteComponent,
                                sprite,
                                $"{layerState}_{group}_{_thresholds[1]}",
                                $"{layer}{group}",
                                index);
                        }
                        _disabledLayers.Add(layer, false);
                    }
                    else if (_damageOverlay != null)
                    {
                        AddDamageLayerToSprite(spriteComponent,
                            _damageOverlay,
                            $"{layerState}_{_thresholds[1]}",
                            $"{layer}trackDamage",
                            index);
                        _disabledLayers.Add(layer, false);
                    }
                }
            }
            else
            {
                if (_damageOverlayGroups != null)
                {
                    foreach (var (group, sprite) in _damageOverlayGroups)
                    {
                        AddDamageLayerToSprite(spriteComponent,
                            sprite,
                            $"DamageOverlay_{group}_{_thresholds[1]}",
                            $"DamageOverlay{group}");
                        _topMostLayerKey = $"DamageOverlay{group}";
                    }
                }
                else if (_damageOverlay != null)
                {
                    AddDamageLayerToSprite(spriteComponent,
                        _damageOverlay,
                        $"DamageOverlay_{_thresholds[1]}",
                        "DamageOverlay");
                    _topMostLayerKey = $"DamageOverlay";
                }
            }
        }

        private void AddDamageLayerToSprite(SpriteComponent spriteComponent, DamageVisualizerSprite sprite, string state, string mapKey, int? index = null)
        {
            int newLayer = spriteComponent.AddLayer(
                new SpriteSpecifier.Rsi(
                    new ResourcePath(sprite.Sprite), state
                ), index);
            spriteComponent.LayerMapSet(mapKey, newLayer);
            if (sprite.Color != null)
                spriteComponent.LayerSetColor(newLayer, Color.FromHex(sprite.Color));
            spriteComponent.LayerSetVisible(newLayer, false);
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

            if (_targetLayers != null && _damageOverlayGroups != null)
                UpdateDisabledLayers(spriteComponent, component);

            if (_overlay && _damageOverlayGroups != null && _targetLayers == null)
                CheckOverlayOrdering(spriteComponent);

            if (component.TryGetData<bool>(DamageVisualizerKeys.ForceUpdate, out bool update)
                && update)
            {
                ForceUpdateLayers(damageComponent, spriteComponent);
                return;
            }

            if (_trackAllDamage)
                UpdateDamageVisuals(damageComponent, spriteComponent);

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
                    if (!_trackAllDamage && _damageOverlayGroups != null)
                        foreach (string damageGroup in _damageOverlayGroups!.Keys)
                            spriteComponent.LayerSetVisible($"{layer}{damageGroup}", _disabledLayers[layer]);
                    else if (_trackAllDamage)
                        spriteComponent.LayerSetVisible($"{layer}trackDamage", _disabledLayers[layer]);
                }
            }
        }

        private void CheckOverlayOrdering(SpriteComponent spriteComponent)
        {
            if (spriteComponent[_topMostLayerKey] != spriteComponent[spriteComponent.AllLayers.Count() - 1])
            {
                if (!_trackAllDamage && _damageOverlayGroups != null)
                {
                    foreach (var (damageGroup, sprite) in _damageOverlayGroups)
                    {
                        int threshold = _lastThresholdPerGroup[damageGroup];
                        ReorderOverlaySprite(spriteComponent,
                            sprite,
                            $"DamageOverlay{damageGroup}",
                            $"DamageOverlay_{damageGroup}",
                            threshold);
                    }
                }
                else if (_trackAllDamage && _damageOverlay != null)
                {
                    ReorderOverlaySprite(spriteComponent,
                        _damageOverlay,
                        $"DamageOverlay",
                        $"DamageOverlay",
                        _lastDamageThreshold);
                }
            }
        }

        private void ReorderOverlaySprite(SpriteComponent spriteComponent, DamageVisualizerSprite sprite, string key, string statePrefix, int threshold)
        {
            Logger.DebugS("DamageVisualizer", "Attempting to re-order overlay to top.");
            spriteComponent.LayerMapTryGet(key, out int spriteLayer);
            bool visibility = spriteComponent[spriteLayer].Visible;
            spriteComponent.RemoveLayer(spriteLayer);
            if (threshold == 0) // these should automatically be invisible
                threshold = _thresholds[1];
            spriteLayer = spriteComponent.AddLayer(
                new SpriteSpecifier.Rsi(
                    new ResourcePath(sprite.Sprite),
                    $"{statePrefix}_{threshold}"
                ),
                spriteLayer);
            spriteComponent.LayerMapSet(key, spriteLayer);
            spriteComponent.LayerSetVisible(spriteLayer, visibility);
            // this is somewhat iffy since it constantly reallocates
            _topMostLayerKey = key;

        }

        private void UpdateDamageVisuals(DamageableComponent damageComponent, SpriteComponent spriteComponent)
        {
            int damageTotal = (int) Math.Floor(damageComponent.TotalDamage / _divisor);

            int threshold = 0;
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

            if (threshold == _lastDamageThreshold)
                return;

            _lastDamageThreshold = threshold;

            if (_targetLayers != null)
            {
                Logger.DebugS("DamageVisualizer", "Attempting to set target layers now.");
                foreach (var layerMapKey in _targetLayerMapKeys)
                    UpdateTargetLayer(spriteComponent, layerMapKey, threshold);
            }
            else
            {
                Logger.DebugS("DamageVisualizer", "Attempting to set overlay now.");
                UpdateOverlay(spriteComponent, threshold);
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

                // yeah...
                damageTotal = (int) Math.Floor(damageTotal / _divisor);

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
            if (_damageOverlayGroups != null)
            {
                UpdateDamageVisuals(_damageOverlayGroups.Keys.ToList(), damageComponent, spriteComponent);
            }
            else if (_damageGroup != null)
            {
                UpdateDamageVisuals(new List<string>(){ _damageGroup }, damageComponent, spriteComponent);
            }
        }

        private void UpdateTargetLayer(SpriteComponent spriteComponent, object layerMapKey, int threshold)
        {
            if (_overlay && _damageOverlayGroups != null)
            {
                if (!_disabledLayers[layerMapKey])
                {
                    string layerState = _layerMapKeyStates[layerMapKey];
                    spriteComponent.LayerMapTryGet($"{layerMapKey}trackDamage", out int spriteLayer);

                    UpdateDamageLayerState(spriteComponent,
                        spriteLayer,
                        $"{layerState}",
                        threshold);
                }
            }
            else if (!_overlay)
            {
                string layerState = _layerMapKeyStates[layerMapKey];
                spriteComponent.LayerMapTryGet(layerMapKey, out int spriteLayer);

                UpdateDamageLayerState(spriteComponent,
                    spriteLayer,
                    $"{layerState}",
                    threshold);
            }
        }

        private void UpdateTargetLayer(SpriteComponent spriteComponent, object layerMapKey, string damageGroup, int threshold)
        {
            if (_overlay && _damageOverlayGroups != null)
            {
                if (_damageOverlayGroups.ContainsKey(damageGroup) && !_disabledLayers[layerMapKey])
                {
                    string layerState = _layerMapKeyStates[layerMapKey];
                    spriteComponent.LayerMapTryGet($"{layerMapKey}{damageGroup}", out int spriteLayer);

                    UpdateDamageLayerState(spriteComponent,
                        spriteLayer,
                        $"{layerState}_{damageGroup}",
                        threshold);
                }
            }
            else if (!_overlay)
            {
                string layerState = _layerMapKeyStates[layerMapKey];
                spriteComponent.LayerMapTryGet(layerMapKey, out int spriteLayer);

                UpdateDamageLayerState(spriteComponent,
                    spriteLayer,
                    $"{layerState}_{damageGroup}",
                    threshold);
            }
        }

        private void UpdateOverlay(SpriteComponent spriteComponent, int threshold)
        {
            spriteComponent.LayerMapTryGet($"DamageOverlay", out int spriteLayer);

            UpdateDamageLayerState(spriteComponent,
                spriteLayer,
                $"DamageOverlay",
                threshold);
        }

        private void UpdateOverlay(SpriteComponent spriteComponent, string damageGroup, int threshold)
        {
            if (_damageOverlayGroups != null)
            {
                if (_damageOverlayGroups.ContainsKey(damageGroup))
                {
                    spriteComponent.LayerMapTryGet($"DamageOverlay{damageGroup}", out int spriteLayer);

                    UpdateDamageLayerState(spriteComponent,
                        spriteLayer,
                        $"DamageOverlay_{damageGroup}",
                        threshold);
                }
            }
        }

        private void UpdateDamageLayerState(SpriteComponent spriteComponent, int spriteLayer, string statePrefix, int threshold)
        {
            if (threshold == 0)
            {
                spriteComponent.LayerSetVisible(spriteLayer, false);
            }
            else
            {
                if (!spriteComponent[spriteLayer].Visible)
                {
                    spriteComponent.LayerSetVisible(spriteLayer, true);
                }
                spriteComponent.LayerSetState(spriteLayer, $"{statePrefix}_{threshold}");
            }
        }
    }
}
