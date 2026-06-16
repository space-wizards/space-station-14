using System.Linq;
using Content.Client.DisplacementMap;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.DisplacementMap;

namespace Content.Client.Body;

public sealed partial class VisualBodySystem : SharedVisualBodySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private MarkingManager _marking = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualOrganComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<VisualOrganComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<VisualOrganComponent, AfterAutoHandleStateEvent>(OnOrganState);

        SubscribeLocalEvent<VisualOrganMarkingsComponent, OrganGotInsertedEvent>(OnMarkingsGotInserted);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, OrganGotRemovedEvent>(OnMarkingsGotRemoved);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, AfterAutoHandleStateEvent>(OnMarkingsState);

        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<HumanoidLayerVisibilityChangedEvent>>(OnMarkingsChangedVisibility);

        Subs.CVar(_cfg, CCVars.AccessibilityClientCensorNudity, OnCensorshipChanged, true);
        Subs.CVar(_cfg, CCVars.AccessibilityServerCensorNudity, OnCensorshipChanged, true);
    }

    private void OnCensorshipChanged(bool value)
    {
        var query = AllEntityQuery<OrganComponent, VisualOrganMarkingsComponent>();
        while (query.MoveNext(out var ent, out var organComp, out var markingsComp))
        {
            if (organComp.Body is not { } body)
                continue;

            RemoveMarkings((ent, markingsComp), body);
            ApplyMarkings((ent, markingsComp), body);
        }
    }

    private void OnOrganGotInserted(Entity<VisualOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyVisual(ent, args.Target);
    }

    private void OnOrganGotRemoved(Entity<VisualOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveVisual(ent, args.Target);
    }

    private void OnOrganState(Entity<VisualOrganComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private void ApplyVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetData(target, index, ent.Comp.Data);
    }

    private void RemoveVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
    }

    private void OnMarkingsGotInserted(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyMarkings(ent, args.Target);
    }

    private void OnMarkingsGotRemoved(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveMarkings(ent, args.Target);
    }

    private void OnMarkingsState(Entity<VisualOrganMarkingsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    protected override void SetOrganColor(Entity<VisualOrganComponent> ent, Color color)
    {
        base.SetOrganColor(ent, color);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    protected override void SetOrganMarkings(Entity<VisualOrganMarkingsComponent> ent, Dictionary<HumanoidVisualLayers, List<Marking>> markings)
    {
        base.SetOrganMarkings(ent, markings);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    protected override void SetOrganAppearance(Entity<VisualOrganComponent> ent, PrototypeLayerData data)
    {
        base.SetOrganAppearance(ent, data);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private IEnumerable<Marking> AllMarkings(Entity<VisualOrganMarkingsComponent> ent)
    {
        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                yield return marking;
            }
        }

        var censorNudity = _cfg.GetCVar(CCVars.AccessibilityClientCensorNudity) || _cfg.GetCVar(CCVars.AccessibilityServerCensorNudity);
        if (!censorNudity)
            yield break;

        var group = _prototype.Index(ent.Comp.MarkingData.Group);
        foreach (var layer in ent.Comp.MarkingData.Layers)
        {
            if (!group.Limits.TryGetValue(layer, out var layerLimits))
                continue;

            if (layerLimits.NudityDefault.Count < 1)
                continue;

            var markings = ent.Comp.Markings.GetValueOrDefault(layer) ?? [];
            if (markings.Any(marking => _marking.TryGetMarking(marking, out var proto) && proto.BodyPart == layer))
                continue;

            foreach (var marking in layerLimits.NudityDefault)
            {
                yield return new(marking, 1);
            }
        }
    }

    private void ApplyMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        var applied = new List<Marking>();
        foreach (var marking in AllMarkings(ent))
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (!_sprite.LayerMapTryGet(target, proto.BodyPart, out var index, true))
                continue;

            ent.Comp.MarkingsDisplacement.TryGetValue(proto.BodyPart, out var displacement);

            if (proto.UsesLayers())
                ApplyMarkingLayers(target, marking, index, displacement, proto);
            else
                ApplyOldMarkingSprites(target, marking, index, displacement, proto);

            applied.Add(marking);
        }
        ent.Comp.AppliedMarkings = applied;
    }

    /// <summary>
    ///     Apply marking layer sprites to a target entity from a prototype.
    /// </summary>
    /// <param name="target">The entity to apply the marking.</param>
    /// <param name="marking">The marking's colors and configuration data.</param>
    /// <param name="index">The index of the body part layer on the entity's sprite stack.</param>
    /// <param name="displacement">Optional displacement data associated with this entity.</param>
    /// <param name="proto">The marking prototype to use.</param>
    private void ApplyMarkingLayers(Entity<SpriteComponent?> target,
        Marking marking,
        int index,
        DisplacementData? displacement,
        MarkingPrototype proto)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        for (var i = 0; i < proto.Layers.Count; i++)
        {
            var layer = proto.Layers[i];
            var layerId = layer.GetLayerID(markingId: proto.ID);
            var sprite = layer.Sprite;
            var spriteLayerIndex = index + i + 1;

            // Add the marking layer to the target entity, if the target doesn't have it yet.
            if (!_sprite.LayerMapTryGet(target, layerId, out _, logMissing: false))
            {
                var spriteLayer = _sprite.AddLayer(target, sprite, newIndex: spriteLayerIndex);
                _sprite.LayerMapSet(target, layerId, spriteLayer);
                _sprite.LayerSetSprite(target, layerId, sprite);
            }

            // Set the color layer to the marking layer state color, or white if nonexistent.
            var layerColor = Color.White;
            if (i < marking.MarkingColors?.Count)
                layerColor = marking.MarkingColors[i];
            _sprite.LayerSetColor(target, layerId, layerColor);

            // Apply displacements.
            if (displacement != null && proto.CanBeDisplaced)
                _displacement.TryAddDisplacement(displacement,
                    sprite: (target, target.Comp),
                    index: spriteLayerIndex,
                    key: layerId,
                    out _);
        }
    }

    private void ApplyOldMarkingSprites(Entity<SpriteComponent?> target,
        Marking marking,
        int index,
        DisplacementData? displacement,
        MarkingPrototype proto)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        for (var i = 0; i < proto.Sprites.Count; i++)
        {
            var sprite = proto.Sprites[i];

            DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
            if (sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{proto.ID}-{rsi.RsiState}";

            if (!_sprite.LayerMapTryGet(target, layerId, out _, false))
            {
                var spriteLayer = _sprite.AddLayer(target, sprite, index + i + 1);
                _sprite.LayerMapSet(target, layerId, spriteLayer);
                _sprite.LayerSetSprite(target, layerId, rsi);
            }

            if (marking.MarkingColors is not null && i < marking.MarkingColors.Count)
                _sprite.LayerSetColor(target, layerId, marking.MarkingColors[i]);
            else
                _sprite.LayerSetColor(target, layerId, Color.White);

            if (displacement != null && proto.CanBeDisplaced)
                _displacement.TryAddDisplacement(displacement, (target, target.Comp), index + i + 1, layerId, out _);
        }
    }

    private void RemoveMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        foreach (var marking in ent.Comp.AppliedMarkings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (proto.UsesLayers())
                RemoveMarkingLayers(target, proto);
            else
                RemoveOldMarkingSprites(target, proto);
        }
    }

    /// <summary>
    ///     Removes sprites from a target entity associated with a marking prototype.
    /// </summary>
    /// <param name="target">The entity to remove a marking from.</param>
    /// <param name="proto">The marking prototype to remove.</param>
    private void RemoveMarkingLayers(Entity<SpriteComponent?> target, MarkingPrototype proto)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        foreach (var layer in proto.Layers)
        {
            var layerId = layer.GetLayerID(proto.ID);
            RemoveMarkingSprite(target, layerId, proto);
        }
    }

    private void RemoveOldMarkingSprites(Entity<SpriteComponent?> target, MarkingPrototype proto)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        foreach (var sprite in proto.Sprites)
        {
            DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
            if (sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{proto.ID}-{rsi.RsiState}";
            RemoveMarkingSprite(target, layerId, proto);
        }
    }

    private void RemoveMarkingSprite(Entity<SpriteComponent?> target, string layerId, MarkingPrototype proto)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        // If this marking is one that can be displaced, we need to remove the displacement as well; otherwise
        // altering a marking at runtime can lead to the renderer falling over.
        // The Vulps must be shaved.
        // (https://github.com/space-wizards/space-station-14/issues/40135).
        if (proto.CanBeDisplaced)
            _displacement.EnsureDisplacementIsNotOnSprite((target, target.Comp), layerId);

        if (!_sprite.LayerMapTryGet(target, layerId, out var index, false))
            return;

        _sprite.LayerMapRemove(target, layerId);
        _sprite.RemoveLayer(target, index);
    }

    private void OnMarkingsChangedVisibility(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<HumanoidLayerVisibilityChangedEvent> args)
    {
        if (!ent.Comp.HideableLayers.Contains(args.Args.Layer))
            return;

        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                if (proto.BodyPart != args.Args.Layer && !(ent.Comp.DependentHidingLayers.TryGetValue(args.Args.Layer, out var dependent) && dependent.Contains(proto.BodyPart)))
                    continue;

                // TODO
                foreach (var sprite in proto.Sprites)
                {
                    DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    if (!_sprite.LayerMapTryGet(args.Body.Owner, layerId, out var index, true))
                        continue;

                    _sprite.LayerSetVisible(args.Body.Owner, index, args.Args.Visible);
                }
            }
        }
    }
}
