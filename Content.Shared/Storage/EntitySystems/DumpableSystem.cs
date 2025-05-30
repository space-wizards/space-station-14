using System.Linq;
using Content.Shared._DV.SmartFridge; // DeltaV - ough why do you not use events for this
using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Placeable;
using Content.Shared.Storage.Components;
using Content.Shared.Tag; // DeltaV - ough why do you not use events for this
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

public sealed class DumpableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDisposalUnitSystem _disposalUnitSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!; // DeltaV - ough why do you not use events for this
    [Dependency] private readonly SmartFridgeSystem _smartFridge = default!; // DeltaV - ough why do you not use events for this
    [Dependency] private readonly TagSystem _tag = default!; // DeltaV - ough why do you not use events for this

    private EntityQuery<ItemComponent> _itemQuery;

    public override void Initialize()
    {
        base.Initialize();
        _itemQuery = GetEntityQuery<ItemComponent>();
        SubscribeLocalEvent<DumpableComponent, AfterInteractEvent>(OnAfterInteract, after: new[]{ typeof(SharedEntityStorageSystem) });
        SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<AlternativeVerb>>(AddDumpVerb);
        SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<UtilityVerb>>(AddUtilityVerbs);
        SubscribeLocalEvent<DumpableComponent, DumpableDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, DumpableComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (!HasComp<DisposalUnitComponent>(args.Target) &&
            !HasComp<PlaceableSurfaceComponent>(args.Target) &&
            !HasComp<SmartFridgeComponent>(args.Target)) // DeltaV - ough why do you not use events for this
            return;

        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        if (!storage.Container.ContainedEntities.Any())
            return;

        StartDoAfter(uid, args.Target.Value, args.User, component);
        args.Handled = true;
    }

    private void AddDumpVerb(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage) || !storage.Container.ContainedEntities.Any())
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                StartDoAfter(uid, args.Target, args.User, dumpable);//Had multiplier of 0.6f
            },
            Text = Loc.GetString("dump-verb-name"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/drop.svg.192dpi.png")),
        };
        args.Verbs.Add(verb);
    }

    private void AddUtilityVerbs(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage) || !storage.Container.ContainedEntities.Any())
            return;

        if (HasComp<DisposalUnitComponent>(args.Target) || HasComp<SmartFridgeComponent>(args.Target)) // DeltaV - ough why do you not use events for this
        {
            UtilityVerb verb = new()
            {
                Act = () =>
                {
                    StartDoAfter(uid, args.Target, args.User, dumpable);
                },
                Text = Loc.GetString("dump-disposal-verb-name", ("unit", args.Target)),
                IconEntity = GetNetEntity(uid)
            };
            args.Verbs.Add(verb);
        }

        if (HasComp<PlaceableSurfaceComponent>(args.Target))
        {
            UtilityVerb verb = new()
            {
                Act = () =>
                {
                    StartDoAfter(uid, args.Target, args.User, dumpable);
                },
                Text = Loc.GetString("dump-placeable-verb-name", ("surface", args.Target)),
                IconEntity = GetNetEntity(uid)
            };
            args.Verbs.Add(verb);
        }
    }

    private void StartDoAfter(EntityUid storageUid, EntityUid targetUid, EntityUid userUid, DumpableComponent dumpable)
    {
        if (!TryComp<StorageComponent>(storageUid, out var storage))
            return;

        // Begin DeltaV - ough why do you not use events for this
        if (HasComp<SmartFridgeComponent>(targetUid) && _tag.HasTag(storageUid, "SmartFridgeRetainStorage"))
            return;
        // End DeltaV - ough why do you not use events for this

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

        delay *= (float) dumpable.DelayPerItem.TotalSeconds * dumpable.Multiplier;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, userUid, delay, new DumpableDoAfterEvent(), storageUid, target: targetUid, used: storageUid)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(EntityUid uid, DumpableComponent component, DumpableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !TryComp<StorageComponent>(uid, out var storage) || storage.Container.ContainedEntities.Count == 0)
            return;

        var dumpQueue = new Queue<EntityUid>(storage.Container.ContainedEntities);

        var dumped = false;

        if (HasComp<DisposalUnitComponent>(args.Args.Target))
        {
            dumped = true;

            foreach (var entity in dumpQueue)
            {
                _disposalUnitSystem.DoInsertDisposalUnit(args.Args.Target.Value, entity, args.Args.User);
            }
        }
        else if (HasComp<PlaceableSurfaceComponent>(args.Args.Target))
        {
            dumped = true;

            var (targetPos, targetRot) = _transformSystem.GetWorldPositionRotation(args.Args.Target.Value);

            foreach (var entity in dumpQueue)
            {
                _transformSystem.SetWorldPositionRotation(entity, targetPos + _random.NextVector2Box() / 4, targetRot);
            }
        }
        // Begin DeltaV - ough why do you not use events for this
        else if (TryComp<SmartFridgeComponent>(args.Args.Target, out var fridge))
        {
            dumped = true;

            if (_container.TryGetContainer(args.Args.Target.Value, fridge.Container, out var container))
            {
                foreach (var entity in dumpQueue)
                {
                    _container.Insert(entity, container);
                    _smartFridge.AddListing((args.Args.Target.Value, fridge), entity, container);
                    Dirty(args.Args.Target.Value, fridge);
                }
            }
        }
        // End DeltaV - ough why do you not use events for this
        else
        {
            var targetPos = _transformSystem.GetWorldPosition(uid);

            foreach (var entity in dumpQueue)
            {
                var transform = Transform(entity);
                _transformSystem.SetWorldPositionRotation(entity, targetPos + _random.NextVector2Box() / 4, _random.NextAngle(), transform);
            }
        }

        if (dumped)
        {
            _audio.PlayPredicted(component.DumpSound, uid, args.User);
        }
    }
}
