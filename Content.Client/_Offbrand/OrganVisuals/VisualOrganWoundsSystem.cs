using Content.Client._Offbrand.BodyVisuals;
using Content.Client.Body;
using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.OrganVisuals;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Offbrand.OrganVisuals;

public sealed partial class VisualOrganWoundsSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Masked";

    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private BodyAppearanceRelaySystem _relay = default!;
    [Dependency] private VisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualOrganWoundsComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<VisualOrganWoundsComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<VisualOrganWoundsComponent, WoundableDamageChanged>(OnWoundableOrganDamageChanged);

        SubscribeLocalEvent<VisualOrganWoundsComponent, BodyRelayedEvent<BodyAppearanceRelayTargetAddedEvent>>(OnRelayTargetAdded);
        SubscribeLocalEvent<VisualOrganWoundsComponent, BodyRelayedEvent<BodyAppearanceRelayTargetRemovedEvent>>(OnRelayTargetRemoved);
    }

    private void OnOrganGotInserted(Entity<VisualOrganWoundsComponent> ent, ref OrganGotInsertedEvent args)
    {
        foreach (var target in _relay.GetTargets(args.Target))
        {
            SetupLayers(ent, target);
            UpdateOverlay(ent, target);
        }

        UpdateOrganOverlay(ent);
    }

    private void OnOrganGotRemoved(Entity<VisualOrganWoundsComponent> ent, ref OrganGotRemovedEvent args)
    {
        foreach (var target in _relay.GetTargets(args.Target))
        {
            RemoveLayers(ent, target);
        }
    }

    private bool SetupLayers(Entity<VisualOrganWoundsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return false;

        if (ent.Comp.LayersInitialized.Contains(target.Owner))
            return true;

        var targetSprite = new Entity<SpriteComponent>(target, target.Comp);

        var visualOrgan = Comp<VisualOrganComponent>(ent);
        var organLayer = visualOrgan.Layer;
        if (!_sprite.LayerMapTryGet(target, organLayer, out var baseIndex, false))
        {
            _visualBody.ApplyVisual(ent.Owner, target.Owner);
            if (!_sprite.LayerMapTryGet(target, organLayer, out baseIndex, false))
                return false;
        }

        var layerIndex = baseIndex;

        foreach (var group in ent.Comp.DamageGroups)
        {
            layerIndex++;

            var maskLayerKey = $"{organLayer}-{group.DamageGroup}-mask";
            var overlayLayerKey = $"{organLayer}-{group.DamageGroup}-layer";

            _sprite.AddBlankLayer(targetSprite, layerIndex);
            _sprite.LayerMapSet(target, overlayLayerKey, layerIndex);
            if (group.Color is { } color)
                _sprite.LayerSetColor(target, overlayLayerKey, color);
            target.Comp.LayerSetShader(overlayLayerKey, Shader);

            var maskLayerIdx = _sprite.AddLayer(target, new SpriteSpecifier.Rsi(ent.Comp.MaskPath, organLayer.ToString()));
            _sprite.LayerMapSet(target, maskLayerKey, maskLayerIdx);
            var ok = _sprite.TryGetLayer(target, maskLayerIdx, out var maskLayer, true);
            DebugTools.Assert(ok);

            maskLayer!.CopyToShaderParameters = new SpriteComponent.CopyToShaderParameters(overlayLayerKey)
            {
                ParameterTexture = "uMask",
                ParameterUV = "uMaskUV",
            };
        }

        var bandageLayerKey = $"{organLayer}-bandages";
        layerIndex++;

        _sprite.AddBlankLayer(targetSprite, layerIndex);
        _sprite.LayerMapSet(target, bandageLayerKey, layerIndex);

        ent.Comp.LayersInitialized.Add(target.Owner);
        return true;
    }

    private void RemoveLayers(Entity<VisualOrganWoundsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (!ent.Comp.LayersInitialized.Remove(target.Owner))
            return;

        var visualOrgan = Comp<VisualOrganComponent>(ent);

        foreach (var group in ent.Comp.DamageGroups)
        {
            var maskLayerKey = $"{visualOrgan.Layer}-{group.DamageGroup}-mask";
            var overlayLayerKey = $"{visualOrgan.Layer}-{group.DamageGroup}-layer";

            _sprite.RemoveLayer(target, maskLayerKey);
            _sprite.RemoveLayer(target, overlayLayerKey);
        }

        var bandageLayerKey = $"{visualOrgan.Layer}-bandages";
        _sprite.RemoveLayer(target, bandageLayerKey);
    }

    private void UpdateOverlay(Entity<VisualOrganWoundsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (!TryComp<WoundableComponent>(ent, out var woundable))
            return;

        if (!ent.Comp.LayersInitialized.Contains(target.Owner) && !SetupLayers(ent, target))
            return;

        var visualOrgan = Comp<VisualOrganComponent>(ent);

        foreach (var group in ent.Comp.DamageGroups)
        {
            var overlayLayerKey = $"{visualOrgan.Layer}-{group.DamageGroup}-layer";

            woundable.Damage.TryGetDamageInGroup(_prototype.Index(group.DamageGroup), out var total);
            var thresholdIndex = ent.Comp.Thresholds.BinarySearch(total);

            if (thresholdIndex < -1)
                thresholdIndex = ~thresholdIndex;

            if (thresholdIndex == -1)
            {
                _sprite.LayerSetVisible(target, overlayLayerKey, false);
            }
            else
            {
                if (thresholdIndex >= ent.Comp.Thresholds.Count)
                    thresholdIndex = ent.Comp.Thresholds.Count - 1;

                _sprite.LayerSetRsi(target, overlayLayerKey, group.OverlayPath, new RSI.StateId($"{group.DamageGroup}{thresholdIndex}"));
                _sprite.LayerSetVisible(target, overlayLayerKey, true);
            }
        }

        var bandageLayerKey = $"{visualOrgan.Layer}-bandages";
        var bandageThresholdIndex = ent.Comp.BandageThresholds.BinarySearch(woundable.TendedDamage.GetTotal());

        if (bandageThresholdIndex < -1)
            bandageThresholdIndex = ~bandageThresholdIndex;

        if (bandageThresholdIndex == -1)
        {
            _sprite.LayerSetVisible(target, bandageLayerKey, false);
        }
        else
        {
            _sprite.LayerSetRsi(target, bandageLayerKey, ent.Comp.BandagesPath, new RSI.StateId($"{visualOrgan.Layer}{bandageThresholdIndex}"));
            _sprite.LayerSetVisible(target, bandageLayerKey, true);
        }
    }

    private void UpdateOrganOverlay(Entity<VisualOrganWoundsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        UpdateOverlay(ent, (ent.Owner, sprite));
    }

    private void OnWoundableOrganDamageChanged(Entity<VisualOrganWoundsComponent> ent, ref WoundableDamageChanged args)
    {
        UpdateOrganOverlay(ent);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        foreach (var target in _relay.GetTargets(body))
        {
            UpdateOverlay(ent, target);
        }
    }

    private void OnRelayTargetAdded(Entity<VisualOrganWoundsComponent> ent, ref BodyRelayedEvent<BodyAppearanceRelayTargetAddedEvent> args)
    {
        SetupLayers(ent, args.Args.Target);
        UpdateOverlay(ent, args.Args.Target);
    }

    private void OnRelayTargetRemoved(Entity<VisualOrganWoundsComponent> ent, ref BodyRelayedEvent<BodyAppearanceRelayTargetRemovedEvent> args)
    {
        RemoveLayers(ent, args.Args.Target);
    }
}
