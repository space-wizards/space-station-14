using Content.Client.DisplacementMap;
using Content.Shared.AttachedVisuals;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Client.AttachedVisuals;

public sealed partial class AttachedVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;


    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;
    [Dependency] private EntityQuery<AttachedVisualsComponent> _attachedVisualsQuery = default!;
    [Dependency] private EntityQuery<GenericVisualizerComponent> _genericVisualizerQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<AttachedVisualsComponent> ent, ref ComponentStartup args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<AttachedVisualsComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_attachedVisualsQuery.TryComp(args.Container.Owner, out var attachedVisuals))
            return;

        UpdateVisuals((args.Container.Owner, attachedVisuals));
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<AttachedVisualsComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var origins = new List<EntityUid>();
        GetOrigins(ent, origins);
        RemoveSprites(args.Container.Owner, origins);
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<AttachedVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateVisuals(ent);
    }

    private void GetAttachedVisuals(Entity<AttachedVisualsComponent> ent, VisualAttachmentPrototype attachmentName, string prefix, List<(EntityUid, string, HashSet<string> mapKeys, PrototypeLayerData)> layers)
    {
        if (!ent.Comp.AttachedVisuals.TryGetValue(attachmentName, out var visuals))
            return;

        _genericVisualizerQuery.TryComp(ent, out var visualizer);
        _appearanceQuery.TryComp(ent, out var appearance);

        var ev = new GetAttachedVisualsEvent(attachmentName, prefix, layers);

        foreach (var protoLayer in visuals.Layers)
        {
            var layer = _serialization.CreateCopy(protoLayer, notNullableOverride: true);
            if (layer.RsiPath == null && layer.TexturePath == null)
            {
                layer.RsiPath = ent.Comp.RsiPath;
                if (layer.RsiPath == null)
                    continue;
            }

            var keys = new HashSet<string>();

            if (layer.MapKeys != null)
            {
                if (visualizer != null && appearance != null)
                    ApplyVisualizer(ent, visualizer, appearance, layer);

                keys = layer.MapKeys;
                layer.MapKeys = null;
            }

            ev.AddLayer(ent, keys, layer);
        }

        RaiseLocalEvent(ent, ref ev);
    }

    private void ApplyVisualizer(EntityUid uid,
        GenericVisualizerComponent visualizer,
        AppearanceComponent appearance,
        PrototypeLayerData layer)
    {
        foreach (var (appearanceKey, layerDict) in visualizer.Visuals)
        {
            if (!_appearance.TryGetData(uid, appearanceKey, out object? obj, appearance))
                continue;

            var value = obj.ToString();
            if (string.IsNullOrEmpty(value))
                continue;

            foreach (var (layerKey, patches) in layerDict)
            {
                if (!layer.MapKeys!.Contains(layerKey) || !patches.TryGetValue(value, out var patch))
                    continue;

                layer.State = patch.State ?? layer.State;
                layer.RsiPath = patch.RsiPath ?? layer.RsiPath;
                layer.TexturePath = patch.TexturePath ?? layer.TexturePath;
                layer.Color = patch.Color ?? layer.Color;
                layer.Scale = patch.Scale ?? layer.Scale;
                layer.Visible = patch.Visible ?? layer.Visible;
            }
        }
    }

    /// <summary>
    /// Rebuild this entity's sprite and every holder's above it and below it
    /// </summary>
    public void UpdateVisuals(Entity<AttachedVisualsComponent> ent)
    {
        Rebuild(ent);
        if (_container.TryGetContainingContainer(ent.Owner, out var container) && _attachedVisualsQuery.TryComp(container.Owner, out var containerAttachedVisuals))
        {
            UpdateVisuals((container.Owner, containerAttachedVisuals));
        }
    }

    /// <summary>
    /// Full rebuild of an entities layers all the way down
    /// </summary>
    private void Rebuild(Entity<AttachedVisualsComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || !_spriteQuery.TryComp(ent, out var sprite))
            return;

        foreach (var keys in ent.Comp.RevealedLayers.Values)
        {
            foreach (var key in keys)
            {
                _sprite.RemoveLayer((ent.Owner, sprite), key);
            }
        }
        ent.Comp.RevealedLayers.Clear();

        foreach (var attachment in ent.Comp.Attachments)
        {
            var results = new List<(EntityUid Origin, string Key, HashSet<string> mapKeys, PrototypeLayerData Data)>();
            GetAttachmentLayers(ent, attachment, results);

            // Select displacement maps
            var displacementData = attachment.DisplacementData;
            var equipeeSex = CompOrNull<HumanoidProfileComponent>(ent)?.Sex;
            if (equipeeSex != null
                && attachment.SexedDisplacementData != null
                && attachment.SexedDisplacementData.TryGetValue(equipeeSex.Value, out var sexedDisplacementData))
            {
                displacementData = sexedDisplacementData;
            }

            foreach (var (origin, key, mapKeys, data) in results)
            {
                var index = _sprite.LayerMapReserve((ent.Owner, sprite), key);
                _sprite.LayerSetData((ent.Owner, sprite), index, data);
                ent.Comp.RevealedLayers.GetOrNew(origin).Add(key);

                var addedLayerMap = new Dictionary<object, int>();

                if (displacementData is not null && _displacement.TryAddDisplacement(displacementData, (ent, sprite), index, key, out var displacementKey))
                    ent.Comp.RevealedLayers.GetOrNew(origin).Add(displacementKey);

                foreach (var mapkey in mapKeys)
                {
                    var obj = ParseKey(mapkey);
                    addedLayerMap[obj] = index;
                }

                var ev = new AttachedVisualsUpdatedEvent(ent, addedLayerMap);
                RaiseLocalEvent(origin, ref ev);
            }
        }
    }

    /// <summary>
    /// Gets the layers for a specific attachment on an entity
    /// </summary>
    private void GetAttachmentLayers(Entity<AttachedVisualsComponent> ent, AttachmentDefinition attachment, List<(EntityUid, string, HashSet<string> mapKeys, PrototypeLayerData)> results, string keyPrefix = "attached")
    {
        if (!ProtoMan.Resolve(attachment.Attachment, out var attachmentPrototype))
            return;

        if (!_container.TryGetContainer(ent, attachment.Container, out var container))
            return;

        foreach (var child in container.ContainedEntities)
        {
            if (!_attachedVisualsQuery.TryComp(child, out var childComp))
                continue;

            if (!childComp.AttachedVisuals.TryGetValue(attachmentPrototype, out var def))
                continue;

            var childPrefix = $"{keyPrefix}-{attachment.Container}-{child.Id}";

            GetAttachedVisuals((child, childComp), attachmentPrototype, childPrefix, results);

            foreach (var childAttachment in def.Attachments)
            {
                GetAttachmentLayers((child, childComp), childAttachment, results, childPrefix);
            }
        }
    }

    private object ParseKey(string keyString)
    {
        if (_reflection.TryParseEnumReference(keyString, out var @enum))
            return @enum;

        return keyString;
    }

    /// <summary>
    /// Collecting time
    /// </summary>
    private void GetOrigins(EntityUid uid, List<EntityUid> origins)
    {
        origins.Add(uid);

        if (!TryComp<ContainerManagerComponent>(uid, out var containerComp))
            return;

        foreach (var container in _container.GetAllContainers(uid, containerComp))
        {
            foreach (var child in container.ContainedEntities)
            {
                if (_attachedVisualsQuery.HasComp(child))
                    GetOrigins(child, origins);
            }
        }
    }

    /// <summary>
    /// Remove the sprite layers the that the origins own on this entity and every container owner above it.
    /// </summary>
    private void RemoveSprites(EntityUid uid, List<EntityUid> origins)
    {
        while (true)
        {
            if (_attachedVisualsQuery.TryComp(uid, out var comp) && _spriteQuery.TryComp(uid, out var sprite))
            {
                foreach (var origin in origins)
                {
                    if (!comp.RevealedLayers.Remove(origin, out var keys))
                        continue;

                    foreach (var key in keys)
                    {
                        _sprite.RemoveLayer((uid, sprite), key);
                    }
                }
            }

            if (!_container.TryGetContainingContainer((uid, null, null), out var container))
                return;

            uid = container.Owner;
        }
    }
}
