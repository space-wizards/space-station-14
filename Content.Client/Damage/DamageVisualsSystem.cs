using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Damage;

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
///     Damage layers, if targeting layers, can also be dynamically
///     disabled if needed by passing into SetData, the name/enum
///     of the sprite layer, and then passing in a bool value
///     (true to enable, false to disable).
/// </summary>
public sealed class DamageVisualsSystem : VisualizerSystem<DamageVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageVisualsComponent, ComponentInit>(InitializeEntity);
    }

    private void InitializeEntity(EntityUid entity, DamageVisualsComponent comp, ComponentInit args)
    {
        VerifyVisualizerSetup(entity, comp);

        if (!comp.Valid)
        {
            RemCompDeferred<DamageVisualsComponent>(entity);
            return;
        }

        InitializeVisualizer(entity, comp);
    }

    private void VerifyVisualizerSetup(EntityUid entity, DamageVisualsComponent damageVisComp)
    {
        if (damageVisComp.Thresholds.Count < 1)
        {
            Log.Error($"ThresholdsLookup were invalid for entity {entity}. ThresholdsLookup: {damageVisComp.Thresholds}");
            damageVisComp.Valid = false;
            return;
        }

        if (damageVisComp.Divisor == 0)
        {
            Log.Error($"Divisor for {entity} is set to zero.");
            damageVisComp.Valid = false;
            return;
        }

        if (damageVisComp.Overlay)
        {
            if (damageVisComp.DamageOverlayGroups == null && damageVisComp.DamageOverlay == null)
            {
                Log.Error($"Enabled overlay without defined damage overlay sprites on {entity}.");
                damageVisComp.Valid = false;
                return;
            }

            if (damageVisComp.TrackAllDamage && damageVisComp.DamageOverlay == null)
            {
                Log.Error($"Enabled all damage tracking without a damage overlay sprite on {entity}.");
                damageVisComp.Valid = false;
                return;
            }

            if (!damageVisComp.TrackAllDamage && damageVisComp.DamageOverlay != null)
            {
                Log.Warning($"Disabled all damage tracking with a damage overlay sprite on {entity}.");
                damageVisComp.Valid = false;
                return;
            }


            if (damageVisComp.TrackAllDamage && damageVisComp.DamageOverlayGroups != null)
            {
                Log.Warning($"Enabled all damage tracking with damage overlay groups on {entity}.");
                damageVisComp.Valid = false;
                return;
            }
        }
        else if (!damageVisComp.Overlay)
        {
            if (damageVisComp.TargetLayers == null)
            {
                Log.Error($"Disabled overlay without target layers on {entity}.");
                damageVisComp.Valid = false;
                return;
            }

            if (damageVisComp.DamageOverlayGroups != null || damageVisComp.DamageOverlay != null)
            {
                Log.Error($"Disabled overlay with defined damage overlay sprites on {entity}.");
                damageVisComp.Valid = false;
                return;
            }

            if (damageVisComp.DamageGroup == null)
            {
                Log.Error($"Disabled overlay without defined damage group on {entity}.");
                damageVisComp.Valid = false;
                return;
            }
        }

        if (damageVisComp.DamageOverlayGroups != null && damageVisComp.DamageGroup != null)
        {
            Log.Warning($"Damage overlay sprites and damage group are both defined on {entity}.");
        }

        if (damageVisComp.DamageOverlay != null && damageVisComp.DamageGroup != null)
        {
            Log.Warning($"Damage overlay sprites and damage group are both defined on {entity}.");
        }
    }

    private void InitializeVisualizer(EntityUid entity, DamageVisualsComponent damageVisComp)
    {
        if (!TryComp(entity, out SpriteComponent? spriteComponent)
            || !TryComp<DamageableComponent>(entity, out var damageComponent)
            || !HasComp<AppearanceComponent>(entity))
            return;

        damageVisComp.Thresholds.Add(FixedPoint2.Zero);
        damageVisComp.Thresholds.Sort();

        if (damageVisComp.Thresholds[0] != 0)
        {
            Log.Error($"ThresholdsLookup were invalid for entity {entity}. ThresholdsLookup: {damageVisComp.Thresholds}");
            damageVisComp.Valid = false;
            return;
        }

        // If the damage container on our entity's DamageableComponent
        // is not null, we can try to check through its groups.
        if (damageComponent.DamageContainerID != null
            && _prototypeManager.TryIndex<DamageContainerPrototype>(damageComponent.DamageContainerID, out var damageContainer))
        {
            // Are we using damage overlay sprites by group?
            // Check if the container matches the supported groups,
            // and start caching the last threshold.
            if (damageVisComp.DamageOverlayGroups != null)
            {
                foreach (var damageType in damageVisComp.DamageOverlayGroups.Keys)
                {
                    if (!damageContainer.SupportedGroups.Contains(damageType))
                    {
                        Log.Error($"Damage key {damageType} was invalid for entity {entity}.");
                        damageVisComp.Valid = false;
                        return;
                    }

                    damageVisComp.LastThresholdPerGroup.Add(damageType, FixedPoint2.Zero);
                }
            }
            // Are we tracking a single damage group without overlay instead?
            // See if that group is in our entity's damage container.
            else if (!damageVisComp.Overlay && damageVisComp.DamageGroup != null)
            {
                if (!damageContainer.SupportedGroups.Contains(damageVisComp.DamageGroup))
                {
                    Log.Error($"Damage keys were invalid for entity {entity}.");
                    damageVisComp.Valid = false;
                    return;
                }

                damageVisComp.LastThresholdPerGroup.Add(damageVisComp.DamageGroup, FixedPoint2.Zero);
            }
        }
        // Ditto above, but instead we go through every group.
        else // oh boy! time to enumerate through every single group!
        {
            var damagePrototypeIdList = _prototypeManager.EnumeratePrototypes<DamageGroupPrototype>()
                .Select((p, _) => p.ID)
                .ToList();
            if (damageVisComp.DamageOverlayGroups != null)
            {
                foreach (var damageType in damageVisComp.DamageOverlayGroups.Keys)
                {
                    if (!damagePrototypeIdList.Contains(damageType))
                    {
                        Log.Error($"Damage keys were invalid for entity {entity}.");
                        damageVisComp.Valid = false;
                        return;
                    }
                    damageVisComp.LastThresholdPerGroup.Add(damageType, FixedPoint2.Zero);
                }
            }
            else if (damageVisComp.DamageGroup != null)
            {
                if (!damagePrototypeIdList.Contains(damageVisComp.DamageGroup))
                {
                    Log.Error($"Damage keys were invalid for entity {entity}.");
                    damageVisComp.Valid = false;
                    return;
                }

                damageVisComp.LastThresholdPerGroup.Add(damageVisComp.DamageGroup, FixedPoint2.Zero);
            }
        }

        // If we're targeting any layers, and the amount of
        // layers is greater than zero, we start reserving
        // all the layers needed to track damage groups
        // on the entity.
        if (damageVisComp.TargetLayers is { Count: > 0 })
        {
            // This should ensure that the layers we're targeting
            // are valid for the visualizer's use.
            //
            // If the layer doesn't have a base state, or
            // the layer key just doesn't exist, we skip it.
            foreach (var key in damageVisComp.TargetLayers)
            {
                if (!SpriteSystem.LayerMapTryGet((entity, spriteComponent), key, out var index, false))
                {
                    Log.Warning($"Layer at key {key} was invalid for entity {entity}.");
                    continue;
                }

                damageVisComp.TargetLayerMapKeys.Add(key);
            }

            // Similar to damage overlay groups, if none of the targeted
            // sprite layers could be used, we display an error and
            // invalidate the visualizer without crashing.
            if (damageVisComp.TargetLayerMapKeys.Count == 0)
            {
                Log.Error($"Target layers were invalid for entity {entity}.");
                damageVisComp.Valid = false;
                return;
            }

            // Otherwise, we start reserving layers. Since the filtering
            // loop above ensures that all of these layers are not null,
            // and have valid state IDs, there should be no issues.
            foreach (var layer in damageVisComp.TargetLayerMapKeys)
            {
                var layerCount = spriteComponent.AllLayers.Count();
                var index = SpriteSystem.LayerMapGet((entity, spriteComponent), layer);
                // var layerState = spriteComponent.LayerGetState(index).ToString()!;

                if (index + 1 != layerCount)
                {
                    index += 1;
                }

                damageVisComp.LayerMapKeyStates.Add(layer, layer.ToString());

                // If we're an overlay, and we're targeting groups,
                // we reserve layers per damage group.
                if (damageVisComp.Overlay && damageVisComp.DamageOverlayGroups != null)
                {
                    foreach (var (group, sprite) in damageVisComp.DamageOverlayGroups)
                    {
                        AddDamageLayerToSprite((entity, spriteComponent),
                            sprite,
                            $"{layer}_{group}_{damageVisComp.Thresholds[1]}",
                            $"{layer}{group}",
                            index);
                    }
                    damageVisComp.DisabledLayers.Add(layer, false);
                }
                // If we're not targeting groups, and we're still
                // using an overlay, we instead just add a general
                // overlay that reflects on how much damage
                // was taken.
                else if (damageVisComp.DamageOverlay != null)
                {
                    AddDamageLayerToSprite((entity, spriteComponent),
                        damageVisComp.DamageOverlay,
                        $"{layer}_{damageVisComp.Thresholds[1]}",
                        $"{layer}trackDamage",
                        index);
                    damageVisComp.DisabledLayers.Add(layer, false);
                }
            }
        }
        // If we're not targeting layers, however,
        // we should ensure that we instead
        // reserve it as an overlay.
        else
        {
            if (damageVisComp.DamageOverlayGroups != null)
            {
                foreach (var (group, sprite) in damageVisComp.DamageOverlayGroups)
                {
                    AddDamageLayerToSprite((entity, spriteComponent),
                        sprite,
                        $"DamageOverlay_{group}_{damageVisComp.Thresholds[1]}",
                        $"DamageOverlay{group}");
                    damageVisComp.TopMostLayerKey = $"DamageOverlay{group}";
                }
            }
            else if (damageVisComp.DamageOverlay != null)
            {
                AddDamageLayerToSprite((entity, spriteComponent),
                    damageVisComp.DamageOverlay,
                    $"DamageOverlay_{damageVisComp.Thresholds[1]}",
                    "DamageOverlay");
                damageVisComp.TopMostLayerKey = $"DamageOverlay";
            }
        }
    }

    /// <summary>
    ///     Adds a damage tracking layer to a given sprite component.
    /// </summary>
    private void AddDamageLayerToSprite(Entity<SpriteComponent?> spriteEnt, DamageVisualizerSprite sprite, string state, string mapKey, int? index = null)
    {
        var newLayer = SpriteSystem.AddLayer(
            spriteEnt,
            new SpriteSpecifier.Rsi(
                new(sprite.Sprite), state
            ),
            index
        );
        SpriteSystem.LayerMapSet(spriteEnt, mapKey, newLayer);
        if (sprite.Color != null)
            SpriteSystem.LayerSetColor(spriteEnt, newLayer, Color.FromHex(sprite.Color));
        SpriteSystem.LayerSetVisible(spriteEnt, newLayer, false);
    }

    protected override void OnAppearanceChange(EntityUid uid, DamageVisualsComponent damageVisComp, ref AppearanceChangeEvent args)
    {
        // how is this still here?
        if (!damageVisComp.Valid)
            return;

        // If this was passed into the component, we update
        // the data to ensure that the current disabled
        // bool matches.
        if (AppearanceSystem.TryGetData<bool>(uid, DamageVisualizerKeys.Disabled, out var disabledStatus, args.Component))
            damageVisComp.Disabled = disabledStatus;

        if (damageVisComp.Disabled)
            return;

        HandleDamage(uid, args.Component, damageVisComp);
    }

    private void HandleDamage(EntityUid uid, AppearanceComponent component, DamageVisualsComponent damageVisComp)
    {
        if (!TryComp(uid, out SpriteComponent? spriteComponent)
            || !TryComp(uid, out DamageableComponent? damageComponent))
            return;

        if (damageVisComp.TargetLayers != null && damageVisComp.DamageOverlayGroups != null)
            UpdateDisabledLayers(uid, spriteComponent, component, damageVisComp);

        if (damageVisComp.Overlay && damageVisComp.DamageOverlayGroups != null && damageVisComp.TargetLayers == null)
            CheckOverlayOrdering((uid, spriteComponent), damageVisComp);

        if (AppearanceSystem.TryGetData<bool>(uid, DamageVisualizerKeys.ForceUpdate, out var update, component)
            && update)
        {
            ForceUpdateLayers((uid, damageComponent, spriteComponent, damageVisComp));
            return;
        }

        if (damageVisComp.TrackAllDamage)
        {
            UpdateDamageVisuals((uid, damageComponent, spriteComponent, damageVisComp));
            return;
        }

        if (!AppearanceSystem.TryGetData<DamageVisualizerGroupData>(uid, DamageVisualizerKeys.DamageUpdateGroups,
                out var data, component))
        {
            data = new DamageVisualizerGroupData(Comp<DamageableComponent>(uid).DamagePerGroup.Keys.ToList());
        }

        UpdateDamageVisuals(data.GroupList, (uid, damageComponent, spriteComponent, damageVisComp));
    }

    /// <summary>
    ///     Checks if any layers were disabled in the last
    ///     data update. Disabled layers mean that the
    ///     layer will no longer be visible, or obtain
    ///     any damage updates.
    /// </summary>
    private void UpdateDisabledLayers(EntityUid uid, SpriteComponent spriteComponent, AppearanceComponent component, DamageVisualsComponent damageVisComp)
    {
        foreach (var layer in damageVisComp.TargetLayerMapKeys)
        {
            // I assume this gets set by something like body system if limbs are missing???
            // TODO is this actually used by anything anywhere?
            AppearanceSystem.TryGetData(uid, layer, out bool disabled, component);

            if (damageVisComp.DisabledLayers[layer] == disabled)
                continue;

            damageVisComp.DisabledLayers[layer] = disabled;
            if (damageVisComp.TrackAllDamage)
            {
                SpriteSystem.LayerSetVisible((uid, spriteComponent), $"{layer}trackDamage", !disabled);
                continue;
            }

            if (damageVisComp.DamageOverlayGroups == null)
                continue;

            foreach (var damageGroup in damageVisComp.DamageOverlayGroups.Keys)
            {
                SpriteSystem.LayerSetVisible((uid, spriteComponent), $"{layer}{damageGroup}", !disabled);
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
    private void CheckOverlayOrdering(Entity<SpriteComponent> spriteEnt, DamageVisualsComponent damageVisComp)
    {
        if (spriteEnt.Comp[damageVisComp.TopMostLayerKey] != spriteEnt.Comp[spriteEnt.Comp.AllLayers.Count() - 1])
        {
            if (!damageVisComp.TrackAllDamage && damageVisComp.DamageOverlayGroups != null)
            {
                foreach (var (damageGroup, sprite) in damageVisComp.DamageOverlayGroups)
                {
                    var threshold = damageVisComp.LastThresholdPerGroup[damageGroup];
                    ReorderOverlaySprite(spriteEnt,
                        damageVisComp,
                        sprite,
                        $"DamageOverlay{damageGroup}",
                        $"DamageOverlay_{damageGroup}",
                        threshold);
                }
            }
            else if (damageVisComp.TrackAllDamage && damageVisComp.DamageOverlay != null)
            {
                ReorderOverlaySprite(spriteEnt,
                    damageVisComp,
                    damageVisComp.DamageOverlay,
                    $"DamageOverlay",
                    $"DamageOverlay",
                    damageVisComp.LastDamageThreshold);
            }
        }
    }

    private void ReorderOverlaySprite(Entity<SpriteComponent> spriteEnt, DamageVisualsComponent damageVisComp, DamageVisualizerSprite sprite, string key, string statePrefix, FixedPoint2 threshold)
    {
        SpriteSystem.LayerMapTryGet(spriteEnt.AsNullable(), key, out var spriteLayer, false);
        var visibility = spriteEnt.Comp[spriteLayer].Visible;
        SpriteSystem.RemoveLayer(spriteEnt.AsNullable(), spriteLayer);
        if (threshold == FixedPoint2.Zero) // these should automatically be invisible
            threshold = damageVisComp.Thresholds[1];
        spriteLayer = SpriteSystem.AddLayer(
            spriteEnt.AsNullable(),
            new SpriteSpecifier.Rsi(
                new(sprite.Sprite),
                $"{statePrefix}_{threshold}"
            ),
            spriteLayer);
        SpriteSystem.LayerMapSet(spriteEnt.AsNullable(), key, spriteLayer);
        SpriteSystem.LayerSetVisible(spriteEnt.AsNullable(), spriteLayer, visibility);
        // this is somewhat iffy since it constantly reallocates
        damageVisComp.TopMostLayerKey = key;
    }

    /// <summary>
    ///     Updates damage visuals without tracking
    ///     any damage groups.
    /// </summary>
    private void UpdateDamageVisuals(Entity<DamageableComponent, SpriteComponent, DamageVisualsComponent> entity)
    {
        var damageComponent = entity.Comp1;
        var spriteComponent = entity.Comp2;
        var damageVisComp = entity.Comp3;

        if (!CheckThresholdBoundary(damageComponent.TotalDamage, damageVisComp.LastDamageThreshold, damageVisComp, out var threshold))
            return;

        damageVisComp.LastDamageThreshold = threshold;

        if (damageVisComp.TargetLayers != null)
        {
            foreach (var layerMapKey in damageVisComp.TargetLayerMapKeys)
            {
                UpdateTargetLayer((entity, spriteComponent), damageVisComp, layerMapKey, threshold);
            }
        }
        else
        {
            UpdateOverlay((entity, spriteComponent), threshold);
        }
    }

    /// <summary>
    ///     Updates damage visuals by damage group,
    ///     according to the list of damage groups
    ///     passed into it.
    /// </summary>
    private void UpdateDamageVisuals(List<string> delta, Entity<DamageableComponent, SpriteComponent, DamageVisualsComponent> entity)
    {
        var damageComponent = entity.Comp1;
        var spriteComponent = entity.Comp2;
        var damageVisComp = entity.Comp3;

        foreach (var damageGroup in delta)
        {
            if (!damageVisComp.Overlay && damageGroup != damageVisComp.DamageGroup)
                continue;

            if (!_prototypeManager.TryIndex<DamageGroupPrototype>(damageGroup, out var damageGroupPrototype)
                || !damageComponent.Damage.TryGetDamageInGroup(damageGroupPrototype, out var damageTotal))
                continue;

            if (!damageVisComp.LastThresholdPerGroup.TryGetValue(damageGroup, out var lastThreshold)
                || !CheckThresholdBoundary(damageTotal, lastThreshold, damageVisComp, out var threshold))
                continue;

            damageVisComp.LastThresholdPerGroup[damageGroup] = threshold;

            if (damageVisComp.TargetLayers != null)
            {
                foreach (var layerMapKey in damageVisComp.TargetLayerMapKeys)
                {
                    UpdateTargetLayer((entity, spriteComponent, damageVisComp), layerMapKey, damageGroup, threshold);
                }
            }
            else
            {
                UpdateOverlay((entity, spriteComponent, damageVisComp), damageGroup, threshold);
            }
        }

    }

    /// <summary>
    ///     Checks if a threshold boundary was passed.
    /// </summary>
    private bool CheckThresholdBoundary(FixedPoint2 damageTotal, FixedPoint2 lastThreshold, DamageVisualsComponent damageVisComp, out FixedPoint2 threshold)
    {
        threshold = FixedPoint2.Zero;
        damageTotal = damageTotal / damageVisComp.Divisor;
        var thresholdIndex = damageVisComp.Thresholds.BinarySearch(damageTotal);

        if (thresholdIndex < 0)
        {
            thresholdIndex = ~thresholdIndex;
            threshold = damageVisComp.Thresholds[thresholdIndex - 1];
        }
        else
        {
            threshold = damageVisComp.Thresholds[thresholdIndex];
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
    private void ForceUpdateLayers(Entity<DamageableComponent, SpriteComponent, DamageVisualsComponent> entity)
    {
        var damageVisComp = entity.Comp3;

        if (damageVisComp.DamageOverlayGroups != null)
        {
            UpdateDamageVisuals(damageVisComp.DamageOverlayGroups.Keys.ToList(), entity);
        }
        else if (damageVisComp.DamageGroup != null)
        {
            UpdateDamageVisuals(new List<string>() { damageVisComp.DamageGroup }, entity);
        }
        else if (damageVisComp.DamageOverlay != null)
        {
            UpdateDamageVisuals(entity);
        }
    }

    /// <summary>
    ///     Updates a target layer. Without a damage group passed in,
    ///     it assumes you're updating a layer that is tracking all
    ///     damage.
    /// </summary>
    private void UpdateTargetLayer(Entity<SpriteComponent> spriteEnt, DamageVisualsComponent damageVisComp, object layerMapKey, FixedPoint2 threshold)
    {
        if (damageVisComp.Overlay && damageVisComp.DamageOverlayGroups != null)
        {
            if (!damageVisComp.DisabledLayers[layerMapKey])
            {
                var layerState = damageVisComp.LayerMapKeyStates[layerMapKey];
                SpriteSystem.LayerMapTryGet(spriteEnt.AsNullable(), $"{layerMapKey}trackDamage", out var spriteLayer, false);

                UpdateDamageLayerState(spriteEnt,
                    spriteLayer,
                    $"{layerState}",
                    threshold);
            }
        }
        else if (!damageVisComp.Overlay)
        {
            var layerState = damageVisComp.LayerMapKeyStates[layerMapKey];
            SpriteSystem.LayerMapTryGet(spriteEnt.AsNullable(), $"{layerMapKey}", out var spriteLayer, false);

            UpdateDamageLayerState(spriteEnt,
                spriteLayer,
                $"{layerState}",
                threshold);
        }
    }

    /// <summary>
    ///     Updates a target layer by damage group.
    /// </summary>
    private void UpdateTargetLayer(Entity<SpriteComponent, DamageVisualsComponent> entity, object layerMapKey, string damageGroup, FixedPoint2 threshold)
    {
        var spriteComponent = entity.Comp1;
        var damageVisComp = entity.Comp2;

        if (damageVisComp.Overlay && damageVisComp.DamageOverlayGroups != null)
        {
            if (damageVisComp.DamageOverlayGroups.ContainsKey(damageGroup) && !damageVisComp.DisabledLayers[layerMapKey])
            {
                var layerState = damageVisComp.LayerMapKeyStates[layerMapKey];
                SpriteSystem.LayerMapTryGet((entity, spriteComponent), $"{layerMapKey}{damageGroup}", out var spriteLayer, false);

                UpdateDamageLayerState(
                    (entity, spriteComponent),
                    spriteLayer,
                    $"{layerState}_{damageGroup}",
                    threshold);
            }
        }
        else if (!damageVisComp.Overlay)
        {
            var layerState = damageVisComp.LayerMapKeyStates[layerMapKey];
            SpriteSystem.LayerMapTryGet((entity, spriteComponent), $"{layerMapKey}", out var spriteLayer, false);

            UpdateDamageLayerState(
                (entity, spriteComponent),
                spriteLayer,
                $"{layerState}_{damageGroup}",
                threshold);
        }
    }

    /// <summary>
    ///     Updates an overlay that is tracking all damage.
    /// </summary>
    private void UpdateOverlay(Entity<SpriteComponent> spriteEnt, FixedPoint2 threshold)
    {
        SpriteSystem.LayerMapTryGet(spriteEnt.AsNullable(), $"DamageOverlay", out var spriteLayer, false);

        UpdateDamageLayerState(spriteEnt,
            spriteLayer,
            $"DamageOverlay",
            threshold);
    }

    /// <summary>
    ///     Updates an overlay based on damage group.
    /// </summary>
    private void UpdateOverlay(Entity<SpriteComponent, DamageVisualsComponent> entity, string damageGroup, FixedPoint2 threshold)
    {
        var spriteComponent = entity.Comp1;
        var damageVisComp = entity.Comp2;

        if (damageVisComp.DamageOverlayGroups != null)
        {
            if (damageVisComp.DamageOverlayGroups.ContainsKey(damageGroup))
            {
                SpriteSystem.LayerMapTryGet((entity, spriteComponent), $"DamageOverlay{damageGroup}", out var spriteLayer, false);

                UpdateDamageLayerState(
                    (entity, spriteComponent),
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
    private void UpdateDamageLayerState(Entity<SpriteComponent> spriteEnt, int spriteLayer, string statePrefix, FixedPoint2 threshold)
    {
        if (threshold == 0)
        {
            SpriteSystem.LayerSetVisible(spriteEnt.AsNullable(), spriteLayer, false);
        }
        else
        {
            if (!spriteEnt.Comp[spriteLayer].Visible)
            {
                SpriteSystem.LayerSetVisible(spriteEnt.AsNullable(), spriteLayer, true);
            }
            SpriteSystem.LayerSetRsiState(spriteEnt.AsNullable(), spriteLayer, $"{statePrefix}_{threshold}");
        }
    }
}
