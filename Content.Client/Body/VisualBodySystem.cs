using System.Linq;
using Content.Client._Offbrand.BodyVisuals; // Offbrand
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

namespace Content.Client.Body;

public sealed partial class VisualBodySystem : SharedVisualBodySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;
    [Dependency] private MarkingManager _marking = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private BodyAppearanceRelaySystem _relay = default!; // Offbrand

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

        // Begin Offbrand
        SubscribeLocalEvent<VisualOrganComponent, BodyRelayedEvent<BodyAppearanceRelayTargetAddedEvent>>(OnVisualRelayTargetAdded);
        SubscribeLocalEvent<VisualOrganComponent, BodyRelayedEvent<BodyAppearanceRelayTargetRemovedEvent>>(OnVisualRelayTargetRemoved);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<BodyAppearanceRelayTargetAddedEvent>>(OnMarkingsRelayTargetAdded);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<BodyAppearanceRelayTargetRemovedEvent>>(OnMarkingsRelayTargetRemoved);
        // End Offbrand

        Subs.CVar(_cfg, CCVars.AccessibilityClientCensorNudity, OnCensorshipChanged, true);
        Subs.CVar(_cfg, CCVars.AccessibilityServerCensorNudity, OnCensorshipChanged, true);
    }

    private void OnCensorshipChanged(bool value)
    {
        var query = AllEntityQuery<OrganComponent, VisualOrganMarkingsComponent>();
        while (query.MoveNext(out var ent, out var organComp, out var markingsComp))
        {
            // Begin Offbrand
            RemoveOrganMarkings((ent, markingsComp));
            ApplyOrganMarkings((ent, markingsComp));
            // End Offbrand

            if (organComp.Body is not { } body)
                continue;

            // Begin Offbrand
            foreach (var target in _relay.GetTargets(body))
            {
                RemoveMarkings((ent, markingsComp), target);
                ApplyMarkings((ent, markingsComp), target);
            }
            // End Offbrand
        }
    }

    private void OnOrganGotInserted(Entity<VisualOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        // Begin Offbrand
        foreach (var target in _relay.GetTargets(args.Target))
        {
            ApplyVisual(ent.AsNullable(), target);
        }
        // End Offbrand
    }

    private void OnOrganGotRemoved(Entity<VisualOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // Begin Offbrand
        foreach (var target in _relay.GetTargets(args.Target))
        {
            RemoveVisual(ent, target);
        }
        // End Offbrand
    }

    private void OnOrganState(Entity<VisualOrganComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyOrganVisual(ent); // Offbrand

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        // Begin Offbrand
        foreach (var target in _relay.GetTargets(body))
        {
            ApplyVisual(ent.AsNullable(), target);
        }
        // End Offbrand
    }

    // Begin Offbrand
    public void ApplyVisual(Entity<VisualOrganComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;
    // End Offbrand

        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetData(target, index, ent.Comp.Data);
    }

    // Begin Offbrand
    private void ApplyOrganVisual(Entity<VisualOrganComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var target = new Entity<SpriteComponent?>(ent.Owner, sprite);
        if (_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, false))
        {
            _sprite.LayerSetData(target, index, ent.Comp.Data);
            return;
        }

        if (sprite.AllLayers.Count() != 1)
            return;

        _sprite.LayerSetData(target, 0, ent.Comp.Data);
    }
    // End Offbrand

    private void RemoveVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
    }

    private void OnMarkingsGotInserted(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        // Begin Offbrand
        ApplyOrganMarkings(ent);
        foreach (var target in _relay.GetTargets(args.Target))
        {
            ApplyMarkings(ent, target);
        }
        // End Offbrand
    }

    private void OnMarkingsGotRemoved(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        // Begin Offbrand
        foreach (var target in _relay.GetTargets(args.Target))
        {
            RemoveMarkings(ent, target);
        }
        // End Offbrand
    }

    private void OnMarkingsState(Entity<VisualOrganMarkingsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Begin Offbrand
        RemoveOrganMarkings(ent);
        ApplyOrganMarkings(ent);
        // End Offbrand

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        // Begin Offbrand
        foreach (var target in _relay.GetTargets(body))
        {
            RemoveMarkings(ent, target);
            ApplyMarkings(ent, target);
        }
        // End Offbrand
    }

    protected override void SetOrganColor(Entity<VisualOrganComponent> ent, Color color)
    {
        base.SetOrganColor(ent, color);

        ApplyOrganVisual(ent); // Offbrand

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        // Begin Offbrand
        foreach (var target in _relay.GetTargets(body))
        {
            ApplyVisual(ent.AsNullable(), target);
        }
        // End Offbrand
    }

    protected override void SetOrganMarkings(Entity<VisualOrganMarkingsComponent> ent, Dictionary<HumanoidVisualLayers, List<Marking>> markings)
    {
        base.SetOrganMarkings(ent, markings);

        // Begin Offbrand
        RemoveOrganMarkings(ent);
        ApplyOrganMarkings(ent);
        // End Offbrand

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        // Begin Offbrand
        foreach (var target in _relay.GetTargets(body))
        {
            RemoveMarkings(ent, target);
            ApplyMarkings(ent, target);
        }
        // End Offbrand
    }

    protected override void SetOrganAppearance(Entity<VisualOrganComponent> ent, PrototypeLayerData data)
    {
        base.SetOrganAppearance(ent, data);

        ApplyOrganVisual(ent); // Offbrand

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        // Begin Offbrand
        foreach (var target in _relay.GetTargets(body))
        {
            ApplyVisual(ent.AsNullable(), target);
        }
        // End Offbrand
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

    // Begin Offbrand
    private void ApplyOrganMarkings(Entity<VisualOrganMarkingsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        ApplyMarkings(ent, (ent.Owner, sprite));
    }

    private void RemoveOrganMarkings(Entity<VisualOrganMarkingsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        RemoveMarkings(ent, (ent.Owner, sprite));
    }
    // End Offbrand

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

            applied.Add(marking);
        }
        ent.Comp.AppliedMarkings = applied;
        ent.Comp.AppliedMarkingsByTarget[target.Owner] = applied; // Offbrand
    }

    private void RemoveMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        // Begin Offbrand
        if (!ent.Comp.AppliedMarkingsByTarget.Remove(target.Owner, out var appliedMarkings))
            return;
        // End Offbrand

        foreach (var marking in appliedMarkings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            foreach (var sprite in proto.Sprites)
            {
                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerId = $"{proto.ID}-{rsi.RsiState}";

                // If this marking is one that can be displaced, we need to remove the displacement as well; otherwise
                // altering a marking at runtime can lead to the renderer falling over.
                // The Vulps must be shaved.
                // (https://github.com/space-wizards/space-station-14/issues/40135).
                if (proto.CanBeDisplaced)
                    _displacement.EnsureDisplacementIsNotOnSprite((target, target.Comp), layerId);

                if (!_sprite.LayerMapTryGet(target, layerId, out var index, false))
                    continue;

                _sprite.LayerMapRemove(target, layerId);
                _sprite.RemoveLayer(target, index);
            }
        }
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

                foreach (var sprite in proto.Sprites)
                {
                    DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    // Begin Offbrand
                    foreach (var target in _relay.GetTargets(args.Body.Owner))
                    {
                        if (!_sprite.LayerMapTryGet(target, layerId, out var index, true))
                            continue;

                        _sprite.LayerSetVisible(target, index, args.Args.Visible);
                    }
                    // End Offbrand
                }
            }
        }
    }

    // Begin Offbrand
    private void OnVisualRelayTargetAdded(Entity<VisualOrganComponent> ent, ref BodyRelayedEvent<BodyAppearanceRelayTargetAddedEvent> args)
    {
        ApplyVisual(ent.AsNullable(), args.Args.Target);
    }

    private void OnVisualRelayTargetRemoved(Entity<VisualOrganComponent> ent, ref BodyRelayedEvent<BodyAppearanceRelayTargetRemovedEvent> args)
    {
        RemoveVisual(ent, args.Args.Target);
    }

    private void OnMarkingsRelayTargetAdded(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<BodyAppearanceRelayTargetAddedEvent> args)
    {
        ApplyMarkings(ent, args.Args.Target);
    }

    private void OnMarkingsRelayTargetRemoved(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<BodyAppearanceRelayTargetRemovedEvent> args)
    {
        RemoveMarkings(ent, args.Args.Target);
    }
    // End Offbrand
}
