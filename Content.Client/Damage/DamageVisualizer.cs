using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
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
    public sealed class DamageVisualizer : AppearanceVisualizer
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const string _name = "DamageVisualizer";
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
        private List<FixedPoint2> _thresholds = new();

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
        internal sealed class DamageVisualizerSprite
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

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            IoCManager.InjectDependencies(this);

            var damageData = _entityManager.EnsureComponent<DamageVisualizerDataComponent>(entity);
            VerifyVisualizerSetup(entity, damageData);
            if (damageData.Valid)
                InitializeVisualizer(entity, damageData);
        }

        private void VerifyVisualizerSetup(EntityUid entity, DamageVisualizerDataComponent damageData)
        {
            if (_thresholds.Count < 1)
            {
                Logger.ErrorS(_name, $"Thresholds were invalid for entity {entity}. Thresholds: {_thresholds}");
                damageData.Valid = false;
                return;
            }

            if (_divisor == 0)
            {
                Logger.ErrorS(_name, $"Divisor for {entity} is set to zero.");
                damageData.Valid = false;
                return;
            }

            if (_overlay)
            {
                if (_damageOverlayGroups == null && _damageOverlay == null)
                {
                    Logger.ErrorS(_name, $"Enabled overlay without defined damage overlay sprites on {entity}.");
                    damageData.Valid = false;
                    return;
                }

                if (_trackAllDamage && _damageOverlay == null)
                {
                    Logger.ErrorS(_name, $"Enabled all damage tracking without a damage overlay sprite on {entity}.");
                    damageData.Valid = false;
                    return;
                }

                if (!_trackAllDamage && _damageOverlay != null)
                {
                    Logger.WarningS(_name, $"Disabled all damage tracking with a damage overlay sprite on {entity}.");
                    damageData.Valid = false;
                    return;
                }


                if (_trackAllDamage && _damageOverlayGroups != null)
                {
                    Logger.WarningS(_name, $"Enabled all damage tracking with damage overlay groups on {entity}.");
                    damageData.Valid = false;
                    return;
                }
            }
            else if (!_overlay)
            {
                if (_targetLayers == null)
                {
                    Logger.ErrorS(_name, $"Disabled overlay without target layers on {entity}.");
                    damageData.Valid = false;
                    return;
                }

                if (_damageOverlayGroups != null || _damageOverlay != null)
                {
                    Logger.ErrorS(_name, $"Disabled overlay with defined damage overlay sprites on {entity}.");
                    damageData.Valid = false;
                    return;
                }

                if (_damageGroup == null)
                {
                    Logger.ErrorS(_name, $"Disabled overlay without defined damage group on {entity}.");
                    damageData.Valid = false;
                    return;
                }
            }

            if (_damageOverlayGroups != null && _damageGroup != null)
            {
                Logger.WarningS(_name, $"Damage overlay sprites and damage group are both defined on {entity}.");
            }

            if (_damageOverlay != null && _damageGroup != null)
            {
                Logger.WarningS(_name, $"Damage overlay sprites and damage group are both defined on {entity}.");
            }
        }

        private void InitializeVisualizer(EntityUid entity, DamageVisualizerDataComponent damageData)
        {
            if (!_entityManager.TryGetComponent(entity, out SpriteComponent? spriteComponent)
                || !_entityManager.TryGetComponent<DamageableComponent?>(entity, out var damageComponent)
                || !_entityManager.HasComponent<AppearanceComponent>(entity))
                return;

            _thresholds.Add(FixedPoint2.Zero);
            _thresholds.Sort();

            if (_thresholds[0] != 0)
            {
                Logger.ErrorS(_name, $"Thresholds were invalid for entity {entity}. Thresholds: {_thresholds}");
                damageData.Valid = false;
                return;
            }

            // If the damage container on our entity's DamageableComponent
            // is not null, we can try to check through its groups.
            if (damageComponent.DamageContainerID != null
                && _prototypeManager.TryIndex<DamageContainerPrototype>(damageComponent.DamageContainerID, out var damageContainer))
            {
                // Are we using damage overlay sprites by group?
                // Check if the container matches the supported groups,
                // and start cacheing the last threshold.
                if (_damageOverlayGroups != null)
                {
                    foreach (string damageType in _damageOverlayGroups.Keys)
                    {
                        if (!damageContainer.SupportedGroups.Contains(damageType))
                        {
                            Logger.ErrorS(_name, $"Damage key {damageType} was invalid for entity {entity}.");
                            damageData.Valid = false;
                            return;
                        }

                        damageData.LastThresholdPerGroup.Add(damageType, FixedPoint2.Zero);
                    }
                }
                // Are we tracking a single damage group without overlay instead?
                // See if that group is in our entity's damage container.
                else if (!_overlay && _damageGroup != null)
                {
                    if (!damageContainer.SupportedGroups.Contains(_damageGroup))
                    {
                        Logger.ErrorS(_name, $"Damage keys were invalid for entity {entity}.");
                        damageData.Valid = false;
                        return;
                    }

                    damageData.LastThresholdPerGroup.Add(_damageGroup, FixedPoint2.Zero);
                }
            }
            // Ditto above, but instead we go through every group.
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
                            Logger.ErrorS(_name, $"Damage keys were invalid for entity {entity}.");
                            damageData.Valid = false;
                            return;
                        }
                        damageData.LastThresholdPerGroup.Add(damageType, FixedPoint2.Zero);
                    }
                else if (_damageGroup != null)
                {
                    if (!damagePrototypeIdList.Contains(_damageGroup))
                    {
                        Logger.ErrorS(_name, $"Damage keys were invalid for entity {entity}.");
                        damageData.Valid = false;
                        return;
                    }

                    damageData.LastThresholdPerGroup.Add(_damageGroup, FixedPoint2.Zero);
                }
            }

            // If we're targetting any layers, and the amount of
            // layers is greater than zero, we start reserving
            // all the layers needed to track damage groups
            // on the entity.
            if (_targetLayers != null && _targetLayers.Count > 0)
            {
                // This should ensure that the layers we're targetting
                // are valid for the visualizer's use.
                //
                // If the layer doesn't have a base state, or
                // the layer key just doesn't exist, we skip it.
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
                    {
                        Logger.WarningS(_name, $"Layer at key {key} was invalid for entity {entity}.");
                        continue;
                    }

                    damageData.TargetLayerMapKeys.Add(key);
                };

                // Similar to damage overlay groups, if none of the targetted
                // sprite layers could be used, we display an error and
                // invalidate the visualizer without crashing.
                if (damageData.TargetLayerMapKeys.Count == 0)
                {
                    Logger.ErrorS(_name, $"Target layers were invalid for entity {entity}.");
                    damageData.Valid = false;
                    return;
                }

                // Otherwise, we start reserving layers. Since the filtering
                // loop above ensures that all of these layers are not null,
                // and have valid state IDs, there should be no issues.
                foreach (object layer in damageData.TargetLayerMapKeys)
                {
                    int layerCount = spriteComponent.AllLayers.Count();
                    int index = spriteComponent.LayerMapGet(layer);
                    string layerState = spriteComponent.LayerGetState(index)!.ToString()!;

                    if (index + 1 != layerCount)
                    {
                        index += 1;
                    }

                    damageData.LayerMapKeyStates.Add(layer, layerState);

                    // If we're an overlay, and we're targetting groups,
                    // we reserve layers per damage group.
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
                    // If we're not targetting groups, and we're still
                    // using an overlay, we instead just add a general
                    // overlay that reflects on how much damage
                    // was taken.
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
            // If we're not targetting layers, however,
            // we should ensure that we instead
            // reserve it as an overlay.
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
        }

        /// <summary>
        ///     Adds a damage tracking layer to a given sprite component.
        /// </summary>
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
            var entities = _entityManager;
            if (!entities.TryGetComponent(component.Owner, out DamageVisualizerDataComponent damageData))
                return;

            if (!damageData.Valid)
                return;

            // If this was passed into the component, we update
            // the data to ensure that the current disabled
            // bool matches.
            if (component.TryGetData<bool>(DamageVisualizerKeys.Disabled, out var disabledStatus))
                if (disabledStatus != damageData.Disabled)
                    damageData.Disabled = disabledStatus;

            if (damageData.Disabled)
                return;

            HandleDamage(component, damageData);
        }

        private void HandleDamage(AppearanceComponent component, DamageVisualizerDataComponent damageData)
        {
            var entities = _entityManager;
            if (!entities.TryGetComponent(component.Owner, out SpriteComponent spriteComponent)
                || !entities.TryGetComponent(component.Owner, out DamageableComponent damageComponent))
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
            else if (component.TryGetData(DamageVisualizerKeys.DamageUpdateGroups, out DamageVisualizerGroupData data))
            {
                UpdateDamageVisuals(data.GroupList, damageComponent, spriteComponent, damageData);
            }
        }

        /// <summary>
        ///     Checks if any layers were disabled in the last
        ///     data update. Disabled layers mean that the
        ///     layer will no longer be visible, or obtain
        ///     any damage updates.
        /// </summary>
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

        /// <summary>
        ///     Checks the overlay ordering on the current
        ///     sprite component, compared to the
        ///     data for the visualizer. If the top
        ///     most layer doesn't match, the sprite
        ///     layers are recreated and placed on top.
        /// </summary>
        private void CheckOverlayOrdering(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
        {
            if (spriteComponent[damageData.TopMostLayerKey] != spriteComponent[spriteComponent.AllLayers.Count() - 1])
            {
                if (!_trackAllDamage && _damageOverlayGroups != null)
                {
                    foreach (var (damageGroup, sprite) in _damageOverlayGroups)
                    {
                        FixedPoint2 threshold = damageData.LastThresholdPerGroup[damageGroup];
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

        private void ReorderOverlaySprite(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData, DamageVisualizerSprite sprite, string key, string statePrefix, FixedPoint2 threshold)
        {
            spriteComponent.LayerMapTryGet(key, out int spriteLayer);
            bool visibility = spriteComponent[spriteLayer].Visible;
            spriteComponent.RemoveLayer(spriteLayer);
            if (threshold == FixedPoint2.Zero) // these should automatically be invisible
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

        /// <summary>
        ///     Updates damage visuals without tracking
        ///     any damage groups.
        /// </summary>
        private void UpdateDamageVisuals(DamageableComponent damageComponent, SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
        {
            if (!CheckThresholdBoundary(damageComponent.TotalDamage, damageData.LastDamageThreshold, out FixedPoint2 threshold))
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

        /// <summary>
        ///     Updates damage visuals by damage group,
        ///     according to the list of damage groups
        ///     passed into it.
        /// </summary>
        private void UpdateDamageVisuals(List<string> delta, DamageableComponent damageComponent, SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData)
        {
            foreach (var damageGroup in delta)
            {
                if (!_overlay && damageGroup != _damageGroup)
                    continue;

                if (!_prototypeManager.TryIndex<DamageGroupPrototype>(damageGroup, out var damageGroupPrototype)
                    || !damageComponent.Damage.TryGetDamageInGroup(damageGroupPrototype, out FixedPoint2 damageTotal))
                    continue;

                if (!damageData.LastThresholdPerGroup.TryGetValue(damageGroup, out FixedPoint2 lastThreshold)
                    || !CheckThresholdBoundary(damageTotal, lastThreshold, out FixedPoint2 threshold))
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

        /// <summary>
        ///     Checks if a threshold boundary was passed.
        /// </summary>
        private bool CheckThresholdBoundary(FixedPoint2 damageTotal, FixedPoint2 lastThreshold, out FixedPoint2 threshold)
        {
            threshold = FixedPoint2.Zero;
            damageTotal = damageTotal / _divisor;
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

            if (threshold == lastThreshold)
                return false;

            return true;
        }

        /// <summary>
        ///     This is the entry point for
        ///     forcing an update on all damage layers.
        ///     Does different things depending on
        ///     the configuration of the visualizer.
        /// </summary>
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

        /// <summary>
        ///     Updates a target layer. Without a damage group passed in,
        ///     it assumes you're updating a layer that is tracking all
        ///     damage.
        /// </summary>
        private void UpdateTargetLayer(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData, object layerMapKey, FixedPoint2 threshold)
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

        /// <summary>
        ///     Updates a target layer by damage group.
        /// </summary>
        private void UpdateTargetLayer(SpriteComponent spriteComponent, DamageVisualizerDataComponent damageData, object layerMapKey, string damageGroup, FixedPoint2 threshold)
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

        /// <summary>
        ///     Updates an overlay that is tracking all damage.
        /// </summary>
        private void UpdateOverlay(SpriteComponent spriteComponent, FixedPoint2 threshold)
        {
            spriteComponent.LayerMapTryGet($"DamageOverlay", out int spriteLayer);

            UpdateDamageLayerState(spriteComponent,
                spriteLayer,
                $"DamageOverlay",
                threshold);
        }

        /// <summary>
        ///     Updates an overlay based on damage group.
        /// </summary>
        private void UpdateOverlay(SpriteComponent spriteComponent, string damageGroup, FixedPoint2 threshold)
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

        /// <summary>
        ///     Updates a layer on the sprite by what
        ///     prefix it has (calculated by whatever
        ///     function calls it), and what threshold
        ///     was passed into it.
        /// </summary>
        private void UpdateDamageLayerState(SpriteComponent spriteComponent, int spriteLayer, string statePrefix, FixedPoint2 threshold)
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
