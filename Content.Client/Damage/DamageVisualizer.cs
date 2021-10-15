using System.Collections.Generic;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Damage
{
    public class DamageVisualizer : AppearanceVisualizer
    {
        [DataField("thresholds", required: true)]
        private List<int> _thresholds = new();

        // If target layer(s) are defined, then all damageSprites will
        // automatically set themselves on those layer - if you just
        // want an overlay sprite, keep this undefined.
        [DataField("targetLayers")]
        private readonly List<string>? _targetLayers;

        // Keyed by damage group identifier.
        [DataField("damageSprites", required: true)]
        private readonly Dictionary<string, ResourcePath> _damageSprites = new();

        // you can also just disable this entirely,
        // (set DamageVisualizerKeys.Disabled to true)
        // e.g., if you have an entity state where
        // it can't be repaired but it still
        // has this visualizer/a DamageableComponent

        // this is here to ensure that the last state of the
        // layer's visibility is cached (can't
        // get a layer's visibility otherwise)
        private Dictionary<string, bool> _layerToggles = new();
        // this is here to ensure that layers stay disabled
        private Dictionary<string, bool> _disabledLayers = new();
        private Dictionary<string, int> _lastThresholds = new();

        // in case misvalidation occurs
        private bool _disabledForValidation = false;

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
                // make a note saying that this isn't valid,
                // and disable
                //
                // you need a minimum damage of something
                _disabledForValidation = true;
                return;
            }

            _thresholds.Add(0);
            _thresholds.Sort();

            if (_thresholds[0] != 0)
            {
                // the add and sort will always mean
                // that the lower threshold should be zero,
                // if not, then this isn't valid
                // (negative damage???)

                _disabledForValidation = true;
                return;
            }

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            // ensure that what damage is given here is supported
            // otherwise we have to check every key to ensure
            // that they're all valid
            if (damageComponent.DamageContainerID != null)
            {
                if (prototypeManager.TryIndex<DamageContainerPrototype>(damageComponent.DamageContainerID, out var damageContainer))
                {
                    // this is valid, OmniSharp, why do you Do This
                    foreach (string damageType in _damageSprites.Keys)
                        // I did initially think about doing per type as well,
                        // but I then realized that the API for DamageSpecifiers
                        // doesn't exactly support getting it per type,
                        //
                        // meaning that I would have to either cache the existing
                        // prototype group, or enumerate through them. Unless
                        // somebody finds a way around that, it'll support only
                        // groups for now.

                        if (!damageContainer.SupportedGroups.Contains(damageType))
                        {
                            _damageSprites.Remove(damageType);
                        }
                }
            }
            else // oh boy! time to enumerate through every single group!
            {
                // no clue if there's a way to ensure any of these exist or not
                var damagePrototypeIdList = prototypeManager.EnumeratePrototypes<DamageGroupPrototype>()
                    .Select((p, _) => p.ID)
                    .ToList();
                foreach (string damageType in _damageSprites.Keys)
                    if (!damagePrototypeIdList.Contains(damageType))
                        _damageSprites.Remove(damageType);
            }

            if (_damageSprites.Keys.Count == 0)
            {
                // every key was invalid! this no longer works
                _disabledForValidation = true;
                return;
            }

            if (_targetLayers != null && _targetLayers.Count > 0)
            {
                foreach (var layer in _targetLayers)
                {
                    if (!spriteComponent.LayerMapTryGet(layer, out int index))
                    {
                        foreach (var (group, sprite) in _damageSprites)
                        {
                            if (spriteComponent.LayerGetState(index) == null)
                                continue;

                            string? layerState = spriteComponent.LayerGetState(index).ToString();
                            if (layerState == null)
                                continue;
                            int newLayer = spriteComponent.AddLayer(new SpriteSpecifier.Rsi(sprite, $"{layerState}{group}0"), index + 1);
                            spriteComponent.LayerMapSet($"{layer}{group}", newLayer);
                        }

                    }
                    appearanceComponent.SetData(layer, true);
                    _layerToggles.Add(layer, true);
                    _disabledLayers.Add(layer, false);
                }
            }
            else
            {
                foreach (var (group, sprite) in _damageSprites)
                {
                    int newLayer = spriteComponent.AddLayer(new SpriteSpecifier.Rsi(sprite, $"DamageOverlay{group}0"));
                    spriteComponent.LayerMapSet($"DamageOverlay{group}", newLayer);
                }
                // Compared to the above, this is not toggleable.
                // Disable the visualizer by setting the relevant key
                // to disabled.
            }
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            if (component.GetData<bool>(DamageVisualizerKeys.Disabled)
                || _disabledForValidation)
                return;

            component.TryGetData<DamageSpecifier>(DamageVisualizerKeys.DamageSpecifier, out DamageSpecifier? specifier);

            HandleDamage(component, specifier);
        }

        private void HandleDamage(AppearanceComponent component, DamageSpecifier? specifier)
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

            if (specifier == null
                || !component.Owner.TryGetComponent<DamageableComponent>(out var damageComponent))
                return;

            // we only need the changed groups,
            // DamageSystem only sends the delta
            // between two states as key values
            foreach (var (damageGroup, _) in specifier.DamageDict)
            {
                // logic goes here
                int threshold = 0;
                int thresholdIndex = _thresholds.BinarySearch(damageComponent.DamagePerGroup[damageGroup]);

                if (thresholdIndex < 0)
                {
                    thresholdIndex ^= thresholdIndex;
                    if (damageComponent.DamagePerGroup[damageGroup] < _thresholds[thresholdIndex])
                    {
                        threshold = _thresholds[thresholdIndex - 1];
                    }
                    else
                    {
                        // we've hit the max limit
                        threshold = _thresholds[thresholdIndex];
                    }
                }
                else
                {
                    threshold = _thresholds[thresholdIndex];
                }

                if (_targetLayers != null)
                {
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
                                spriteComponent.LayerSetState(spriteLayer, $"{layerState}{damageGroup}{threshold}");
                            }
                        }
                }
                else
                {
                    if (_damageSprites.ContainsKey(damageGroup))
                    {
                        spriteComponent.LayerMapTryGet($"DamageOverlay{damageGroup}", out int spriteLayer);
                        if (threshold == 0)
                        {
                            spriteComponent.LayerSetVisible(spriteLayer, false);
                        }
                        else
                        {
                            spriteComponent.LayerSetVisible(spriteLayer, true);
                            spriteComponent.LayerSetState(spriteLayer, $"DamageOverlay{damageGroup}{threshold}");
                        }
                    }
                }
            }
        }
    }
}
