using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.OrganVisuals;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Offbrand.OrganVisuals;

public sealed class VisualOrganWoundsSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Masked";

    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualOrganWoundsComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<VisualOrganWoundsComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<VisualOrganWoundsComponent, WoundableOrganDamageChanged>(OnWoundableOrganDamageChanged);
    }

    private void OnOrganGotInserted(Entity<VisualOrganWoundsComponent> ent, ref OrganGotInsertedEvent args)
    {
        SetupLayers(ent, args.Target);
        UpdateOverlay(ent, args.Target);
    }

    private void OnOrganGotRemoved(Entity<VisualOrganWoundsComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveLayers(ent, args.Target);
    }

    private void SetupLayers(Entity<VisualOrganWoundsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;
        var targetSprite = new Entity<SpriteComponent>(target, target.Comp);

        var visualOrgan = Comp<VisualOrganComponent>(ent);
        var organLayer = visualOrgan.Layer;
        var baseIndex = _sprite.LayerMapGet(target, organLayer);
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

        ent.Comp.LayersInitialized = true;
    }

    private void RemoveLayers(Entity<VisualOrganWoundsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
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

        ent.Comp.LayersInitialized = false;
    }

    private void UpdateOverlay(Entity<VisualOrganWoundsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        if (!TryComp<WoundableOrganComponent>(ent, out var woundable))
            return;

        if (!ent.Comp.LayersInitialized)
            SetupLayers(ent, target);

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

    private void OnWoundableOrganDamageChanged(Entity<VisualOrganWoundsComponent> ent, ref WoundableOrganDamageChanged args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        UpdateOverlay(ent, body);
    }
}
