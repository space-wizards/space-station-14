using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Client.GameObjects;
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
        [Dependency] IEntityManager _entityManager = default!;
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

        /// <summary>
        ///     Set this if you want incoming damage to be
        ///     divided.
        /// </summary>
        /// <remarks>
        ///     This is more useful if you have similar
        ///     damage sprites inbetween entities,
        ///     but with different damage thresholds
        ///     and you want to avoid duplicating
        ///     these sprites.
        /// </remarks>
        [DataField("damageDivisor")]
        private float _divisor = 1;

        /// <summary>
        ///     Set this to track all damage, instead of specific groups.
        /// </summary>
        /// <remarks>
        ///     This will only work if you have damageOverlay
        ///     defined - otherwise, it will not work.
        /// </remarks>
        [DataField("trackAllDamage")]
        private readonly bool _trackAllDamage = false;
        /// <summary>
        ///     This is the overlay sprite used, if _trackAllDamage is
        ///     enabled. Supports no complex per-group layering,
        ///     just an actually simple damage overlay. See
        ///     DamageVisualizerSprite for more information.
        /// </summary>
        [DataField("damageOverlay")]
        private readonly DamageVisualizerSprite? _damageOverlay;

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
            ///     States in here will require one of four
            ///     forms:
            ///
            ///     If tracking damage groups:
            ///     - {base_state}_{group}_{threshold} if targetting
            ///       a static layer on a sprite (either as an
            ///       overlay or as a state change)
            ///     - DamageOverlay_{group}_{threshold} if not
            ///       targetting a layer on a sprite.
            ///
            ///     If not tracking damage groups:
            ///     - {base_state}_{threshold} if it is targetting
            ///       a layer
            ///     - DamageOverlay_{threshold} if not targetting
            ///       a layer.
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

            var damageData = _entityManager.AddComponent<DamageVisualizerDataComponent>(entity);

            _thresholds.Add(0);
            _thresholds.Sort();

            if (_thresholds[0] != 0)
            {
                Logger.ErrorS("DamageVisualizer", $"Thresholds were invalid for entity {entity.Name}. Thresholds: {_thresholds}");
                _valid = false;
                return;
            }

            if (damageComponent.DamageContainerID != null
                && _prototypeManager.TryIndex<DamageContainerPrototype>(damageComponent.DamageContainerID, out var damageContainer))
            {
                if (_damageOverlayGroups != null)
                {
                    foreach (string damageType in _damageOverlayGroups.Keys)
                    {
                        if (!damageContainer.SupportedGroups.Contains(damageType))
                        {
                            _damageOverlayGroups.Remove(damageType);
                            continue;
                        }
                        damageData.LastThresholdPerGroup.Add(damageType, 0);
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

                    damageData.LastThresholdPerGroup.Add(_damageGroup, 0);
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
                        {
                            _damageOverlayGroups.Remove(damageType);
                            continue;
                        }
                        damageData.LastThresholdPerGroup.Add(damageType, 0);
                    }
                else if (_damageGroup != null)
                {
                    if (!damagePrototypeIdList.Contains(_damageGroup))
                    {
                        Logger.ErrorS("DamageVisualizer", $"Damage keys were invalid for entity {entity.Name}.");
                        _valid = false;
                        return;
                    }

                    damageData.LastThresholdPerGroup.Add(_damageGroup, 0);
                }
            }

            if (_damageOverlayGroups != null
                && _damageOverlayGroups.Keys.Count == 0)
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

                    damageData.TargetLayerMapKeys.Add(key);
                };

                if (damageData.TargetLayerMapKeys.Count == 0)
                {
                    Logger.ErrorS("DamageVisualizer", $"Target layers were invalid for entity {entity.Name}.");
                    _valid = false;
                    return;
                }

                foreach (object layer in damageData.TargetLayerMapKeys)
                {
                    int layerCount = spriteComponent.AllLayers.Count();
                    int index = spriteComponent.LayerMapGet(layer);
                    string layerState = spriteComponent.LayerGetState(index)!.ToString()!;

                    if (index + 1 != layerCount)
                    {
                        index = index + 1;
                    }

                    damageData.LayerMapKeyStates.Add(layer, layerState);

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
                        damageData.DisabledLayers.Add(layer, false);
                    }
                    else if (_damageOverlay != null)
                    {
                        AddDamageLayerToSprite(spriteComponent,
                            _damageOverlay,
                            $"{layerState}_{_thresholds[1]}",
                            $"{layer}trackDamage",
                            index);
                        damageData.DisabledLayers.Add(layer, false);
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
                        damageData.TopMostLayerKey = $"DamageOverlay{group}";
                    }
                }
                else if (_damageOverlay != null)
                {
                    AddDamageLayerToSprite(spriteComponent,
                        _damageOverlay,
                        $"DamageOverlay_{_thresholds[1]}",
                        "DamageOverlay");
                    damageData.TopMostLayerKey = $"DamageOverlay";
                }
            }

            appearanceComponent.SetData("damageData", damageData);
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

            if (!component.Owner.TryGetComponent<DamageVisualizerDataComponent>(out var damageData))
                return;

            if (component.TryGetData<bool>(DamageVisualizerKeys.Disabled, out var disabledStatus))
                if (disabledStatus != damageData.Disabled)
                    damageData.Disabled = disabledStatus;

            if (damageData.Disabled)
                return;

            HandleDamage(component, damageData);
        }

        private void HandleDamage(AppearanceComponent component, DamageVisualizerDataComponent damageData)
        {
            if (!component.Owner.TryGetComponent<SpriteComponent>(out var spriteComponent)
                || !component.Owner.TryGetComponent<DamageableComponent>(out var damageComponent))
                return;

            if (_targetLayers != null && _damageOverlayGroups != null)
                UpdateDisabledLayers(spriteComponent, component, damageData);

            if (_overlay && _damageOverlayGroups != null && _targetLayers == null)
                CheckOverlayOrdering(spriteComponent, damageData);

            if (component.TryGetData<bool>(DamageVisualizerKeys.ForceUpdate, out bool update)
                && update)
            {
                ForceUpdateLayers(damageComponent, spriteComponent, damageData);
                return;
            }

            if (_trackAllDamage)
            {
                UpdateDamageVisuals(damageComponent, spriteComponent, damageData);
            }
            else if (component.TryGetData<List<string>>(DamageVisualizerKeys.DamageUpdateGroups, out List<string>? delta))
            {
                UpdateDamageVisuals(delta, damageComponent, spriteComponent, damageData);
            }

            component.SetData(component.Owner.Uid.ToString(), damageData);
        }

        private void UpdateDisabledLayers(SpriteComponent spriteComponent, AppearanceComponent component, DamageVisualizerDataComponent damageData)
        {
            foreach (object layer in damageData.TargetLayerMapKeys)
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

                if (damageData.DisabledLayers[layer] != (bool) layerStatus)
                {
                    damageData.DisabledLayers[layer] = (bool) layerStatus;
                    if (!_trackAllDamage && _damageOverlayGroups != null)
                        foreach (string damageGroup in _damageOverlayGroups!.Keys)
                            spriteComponent.LayerSetVisible($"{layer}{damageGroup}", damageData.DisabledLayers[layer]);
                    else if (_trackAllDamage)
                        spriteComponent.LayerSetVisible($"{layer}trackDamage", damageData.DisabledLayers[layer]);
                }
            }
        }

        private void CheckOverlayOrdering(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
        {
            if (spriteComponent[damageData.TopMostLayerKey] != spriteComponent[spriteComponent.AllLayers.Count() - 1])
            {
                if (!_trackAllDamage && _damageOverlayGroups != null)
                {
                    foreach (var (damageGroup, sprite) in _damageOverlayGroups)
                    {
                        int threshold = damageData.LastThresholdPerGroup[damageGroup];
                        ReorderOverlaySprite(spriteComponent,
                            damageData,
                            sprite,
                            $"DamageOverlay{damageGroup}",
                            $"DamageOverlay_{damageGroup}",
                            threshold);
                    }
                }
                else if (_trackAllDamage && _damageOverlay != null)
                {
                    ReorderOverlaySprite(spriteComponent,
                        damageData,
                        _damageOverlay,
                        $"DamageOverlay",
                        $"DamageOverlay",
                        damageData.LastDamageThreshold);
                }
            }
        }

        private void ReorderOverlaySprite(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData, DamageVisualizerSprite sprite, string key, string statePrefix, int threshold)
        {
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
            damageData.TopMostLayerKey = key;
        }

        private void UpdateDamageVisuals(DamageableComponent damageComponent, SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
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

            if (threshold == damageData.LastDamageThreshold)
                return;

            damageData.LastDamageThreshold = threshold;

            if (_targetLayers != null)
            {
                foreach (var layerMapKey in damageData.TargetLayerMapKeys)
                    UpdateTargetLayer(spriteComponent, damageData, layerMapKey, threshold);
            }
            else
            {
                UpdateOverlay(spriteComponent, threshold);
            }
        }

        private void UpdateDamageVisuals(List<string> delta, DamageableComponent damageComponent, SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
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

                if (!damageData.LastThresholdPerGroup.TryGetValue(damageGroup, out int lastThreshold)
                    || threshold == lastThreshold)
                    continue;

                damageData.LastThresholdPerGroup[damageGroup] = threshold;

                if (_targetLayers != null)
                {
                    foreach (var layerMapKey in damageData.TargetLayerMapKeys)
                        UpdateTargetLayer(spriteComponent, damageData, layerMapKey, damageGroup, threshold);
                }
                else
                {
                    UpdateOverlay(spriteComponent, damageGroup, threshold);
                }
            }

        }

        private void ForceUpdateLayers(DamageableComponent damageComponent, SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
        {
            if (_damageOverlayGroups != null)
            {
                UpdateDamageVisuals(_damageOverlayGroups.Keys.ToList(), damageComponent, spriteComponent, damageData);
            }
            else if (_damageGroup != null)
            {
                UpdateDamageVisuals(new List<string>(){ _damageGroup }, damageComponent, spriteComponent, damageData);
            }
            else if (_damageOverlay != null)
            {
                UpdateDamageVisuals(damageComponent, spriteComponent, damageData);
            }
        }

        private void UpdateTargetLayer(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData, object layerMapKey, int threshold)
        {
            if (_overlay && _damageOverlayGroups != null)
            {
                if (!damageData.DisabledLayers[layerMapKey])
                {
                    string layerState = damageData.LayerMapKeyStates[layerMapKey];
                    spriteComponent.LayerMapTryGet($"{layerMapKey}trackDamage", out int spriteLayer);

                    UpdateDamageLayerState(spriteComponent,
                        spriteLayer,
                        $"{layerState}",
                        threshold);
                }
            }
            else if (!_overlay)
            {
                string layerState = damageData.LayerMapKeyStates[layerMapKey];
                spriteComponent.LayerMapTryGet(layerMapKey, out int spriteLayer);

                UpdateDamageLayerState(spriteComponent,
                    spriteLayer,
                    $"{layerState}",
                    threshold);
            }
        }

        private void UpdateTargetLayer(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData, object layerMapKey, string damageGroup, int threshold)
        {
            if (_overlay && _damageOverlayGroups != null)
            {
                if (_damageOverlayGroups.ContainsKey(damageGroup) && !damageData.DisabledLayers[layerMapKey])
                {
                    string layerState = damageData.LayerMapKeyStates[layerMapKey];
                    spriteComponent.LayerMapTryGet($"{layerMapKey}{damageGroup}", out int spriteLayer);

                    UpdateDamageLayerState(spriteComponent,
                        spriteLayer,
                        $"{layerState}_{damageGroup}",
                        threshold);
                }
            }
            else if (!_overlay)
            {
                string layerState = damageData.LayerMapKeyStates[layerMapKey];
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
