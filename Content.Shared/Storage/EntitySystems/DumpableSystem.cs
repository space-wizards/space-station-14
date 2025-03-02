using System.Linq;
using Content.Shared.Disposal;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Materials;
using Content.Shared.Placeable;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.EntitySystems;

public sealed class DumpableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private EntityQuery<ItemComponent> _itemQuery;

    public override void Initialize()
    {
        base.Initialize();
        _itemQuery = GetEntityQuery<ItemComponent>();
        SubscribeLocalEvent<DumpableComponent, AfterInteractEvent>(OnAfterInteract, after: [typeof(SharedEntityStorageSystem)]);
        SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<AlternativeVerb>>(AddDumpVerb);
        SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<UtilityVerb>>(AddUtilityVerbs);
        SubscribeLocalEvent<DumpableComponent, DumpableDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, DumpableComponent comp, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target == null)
            return;

        if (DumpTypeCheck(uid) == 0)
            return;

        if (!TryComp<StorageComponent>(args.Target, out var storage))
            return;

        if (!storage.Container.ContainedEntities.Any())
            return;

        StartDoAfter(uid, args.Target.Value, args.User, comp);
        args.Handled = true;
    }

    private void AddDumpVerb(EntityUid uid, DumpableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage) || !storage.Container.ContainedEntities.Any())
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartDoAfter(uid, args.Target, args.User, comp);//Had multiplier of 0.6f
            },
            Text = Loc.GetString("dump-verb-name"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/drop.svg.192dpi.png")),
        };
        args.Verbs.Add(verb);
    }

    private void AddUtilityVerbs(EntityUid uid, DumpableComponent comp, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage) || !storage.Container.ContainedEntities.Any())
            return;

        var dumpType = DumpTypeCheck(args.Target);

        if (dumpType == DumpTypes.noTarget)
            return;

        if (TryComp<MaterialStorageComponent>(args.Target, out var matComp))
        {
            if (matComp.Whitelist == null || matComp.Whitelist.Tags == null)
                return;

            foreach (var entity in storage.Container.ContainedEntities)
            {
                if (!_tag.HasAnyTag(entity, matComp.Whitelist.Tags))
                    return;
            }
        }

        var locId = "onto";
        if (dumpType == DumpTypes.disposal || dumpType == DumpTypes.materialStorage)
            locId = "into";

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                StartDoAfter(uid, args.Target, args.User, comp);
            },
            Text = Loc.GetString("dump-utility-verb-name", ("target", args.Target), ("into", locId)),
            IconEntity = GetNetEntity(uid)
        };
        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid storageUid, EntityUid targetUid, EntityUid userUid, DumpableComponent comp)
    {
        if (!TryComp<StorageComponent>(storageUid, out var storage))
            return;

        var delay = 0f;

        foreach (var entity in storage.Container.ContainedEntities)
        {
            if (!_itemQuery.TryGetComponent(entity, out var itemComp) ||
                !_prototypeManager.TryIndex(itemComp.Size, out var itemSize))
            {
                continue;
            }

            delay += itemSize.Weight;
        }

        delay *= (float)comp.DelayPerItem.TotalSeconds * comp.Multiplier;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, userUid, delay, new DumpableDoAfterEvent(), storageUid, target: targetUid, used: storageUid)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }
    private void OnDoAfter(EntityUid uid, DumpableComponent comp, DumpableDoAfterEvent args)
    {
        if (args.Handled
        || args.Cancelled
        || args.Args.Target == null
        || !TryComp<StorageComponent>(uid, out var storage)
        || storage.Container.ContainedEntities.Count == 0)
            return;

        var dumpQueue = new Queue<EntityUid>(storage.Container.ContainedEntities);

        var dumpType = DumpTypeCheck(args.Args.Target.Value);

        switch (dumpType)
        {
            case DumpTypes.noTarget:
                var userPos = _transformSystem.GetWorldPosition(uid);
                foreach (var entity in dumpQueue)
                    _transformSystem.SetWorldPositionRotation(entity, userPos + _random.NextVector2Box() / 4, _random.NextAngle(), Transform(entity));
                break;

            case DumpTypes.materialStorage:
                foreach (var entity in dumpQueue)
                    _materialStorage.TryInsertMaterialEntity(args.Args.User, entity, args.Args.Target.Value);
                break;

            case DumpTypes.surface:
                var (targetPos, targetRot) = _transformSystem.GetWorldPositionRotation(args.Args.Target.Value);
                foreach (var entity in dumpQueue)
                    _transformSystem.SetWorldPositionRotation(entity, targetPos + _random.NextVector2Box() / 4, targetRot);
                break;

            case DumpTypes.disposal:
                foreach (var entity in dumpQueue)
                    _disposalUnitSystem.DoInsertDisposalUnit(args.Args.Target.Value, entity, args.Args.User);
                break;
        }

        _audio.PlayPredicted(comp.DumpSound, uid, args.User);
    }

    [Serializable, NetSerializable]
    public enum DumpTypes : sbyte
    {
        noTarget,
        materialStorage,
        surface,
        disposal
    }

    public DumpTypes DumpTypeCheck(EntityUid uid)
    {
        if (_disposalUnitSystem.HasDisposals(uid))
            return DumpTypes.disposal;

        if (HasComp<PlaceableSurfaceComponent>(uid))
            return DumpTypes.surface;

        if (HasComp<MaterialStorageComponent>(uid))
            return DumpTypes.materialStorage;

        return DumpTypes.noTarget;
    }
}
