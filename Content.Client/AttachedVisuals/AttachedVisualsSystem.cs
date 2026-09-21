using System.Linq;
using Content.Shared.AttachedVisuals;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.AttachedVisuals;

public sealed partial class AttachedVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private IReflectionManager _reflection = default!;


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

        if (!_spriteQuery.TryComp(ent, out var sprite))
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
                if (layer.RsiPath == null && sprite.BaseRSI != null)
                {
                    //fuck this wtf??
                    sprite.BaseRSI.Path.TryRelativeTo(SpriteSpecifierSerializer.TextureRoot, out var resPath);
                    layer.RsiPath = resPath.ToString();
                }
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

        var results = new List<(EntityUid Origin, string Key, HashSet<string> mapKeys, PrototypeLayerData Data)>();
        GetLayers(ent, ent.Comp.Attachments, "attached", results);

        foreach (var (origin, key, mapKeys, data) in results)
        {
            var index = _sprite.LayerMapReserve((ent.Owner, sprite), key);
            _sprite.LayerSetData((ent.Owner, sprite), index, data);
            ent.Comp.RevealedLayers.GetOrNew(origin).Add(key);

            var addedLayerMap = new Dictionary<object, int>();

            foreach (var mapkey in mapKeys)
            {
                var obj = ParseKey(mapkey);
                addedLayerMap[obj] = index;
            }

            var ev = new AttachedVisualsUpdatedEvent(ent, addedLayerMap);
            RaiseLocalEvent(origin, ref ev);
        }

    }

    private object ParseKey(string keyString)
    {
        if (_reflection.TryParseEnumReference(keyString, out var @enum))
            return @enum;

        return keyString;
    }

    /// <summary>
    /// Gets the layers all the way down
    /// </summary>
    private void GetLayers(Entity<AttachedVisualsComponent> ent, List<AttachmentDefinition> attachments, string keyPrefix, List<(EntityUid, string, HashSet<string> mapKeys, PrototypeLayerData)> results)
    {
        foreach (var attachment in attachments)
        {
            if (!_container.TryGetContainer(ent, attachment.Container, out var container))
                continue;

            if (!ProtoMan.Resolve(attachment.Attachment, out var attachmentPrototype))
                continue;

            foreach (var child in container.ContainedEntities)
            {
                if (!_attachedVisualsQuery.TryComp(child, out var childComp))
                    continue;

                if (!childComp.AttachedVisuals.TryGetValue(attachmentPrototype, out var def))
                    continue;

                var childPrefix = $"{keyPrefix}-{attachment.Container}-{child.Id}";

                GetAttachedVisuals((child, childComp), attachmentPrototype, childPrefix, results);

                GetLayers((child, childComp), def.Attachments, childPrefix, results);
            }
        }

        //
        //
        // // Sorted so layer order is deterministic between rebuilds and between clients.
        // foreach (var containerId in containers.Containers.Keys.Order())
        // {
        //     if (!attachments.TryGetValue(containerId, out var view))
        //         continue;
        //
        //     if (!ProtoMan.Resolve(view, out var visualAttachmentPrototype))
        //         continue;
        //
        //     foreach (var child in containers.Containers[containerId].ContainedEntities)
        //     {
        //         if (!_attachedVisualsQuery.TryComp(child, out var childComp))
        //             continue;
        //
        //         if (!childComp.AttachedVisuals.TryGetValue(visualAttachmentPrototype, out var def))
        //             continue;
        //
        //         var childPrefix = $"{keyPrefix}-{containerId}-{child.Id}";
        //
        //         GetAttachedVisuals((child, childComp), visualAttachmentPrototype, childPrefix, results);
        //
        //         GetLayers((child, childComp), def.Attachments, childPrefix, results);
        //     }
        // }
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
