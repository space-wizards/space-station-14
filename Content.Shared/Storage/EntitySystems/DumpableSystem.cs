using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage.Components;
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
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

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
        if (!args.CanReach || args.Handled || args.Target is not { } target)
            return;

        var evt = new GetDumpableVerbEvent(args.User, null);
        RaiseLocalEvent(target, ref evt);
        if (evt.Verb is null)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        if (!storage.Container.ContainedEntities.Any())
            return;

        StartDoAfter(uid, target, args.User, component);
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

        var evt = new GetDumpableVerbEvent(args.User, null);
        RaiseLocalEvent(args.Target, ref evt);

        if (evt.Verb is not { } verbText)
            return;

        UtilityVerb verb = new()
        {
            Act = () =>
            {
                StartDoAfter(uid, args.Target, args.User, dumpable);
            },
            Text = verbText,
            IconEntity = GetNetEntity(uid)
        };
        args.Verbs.Add(verb);
    }

    private void StartDoAfter(EntityUid storageUid, EntityUid targetUid, EntityUid userUid, DumpableComponent dumpable)
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

        delay *= (float) dumpable.DelayPerItem.TotalSeconds * dumpable.Multiplier;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, userUid, delay, new DumpableDoAfterEvent(), storageUid, target: targetUid, used: storageUid)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(EntityUid uid, DumpableComponent component, DumpableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !TryComp<StorageComponent>(uid, out var storage) || storage.Container.ContainedEntities.Count == 0 || args.Args.Target is not { } target)
            return;

        var dumpQueue = new Queue<EntityUid>(storage.Container.ContainedEntities);

        var evt = new DumpEvent(dumpQueue, args.Args.User, false, false);
        RaiseLocalEvent(target, ref evt);

        if (!evt.Handled)
        {
            var targetPos = _transformSystem.GetWorldPosition(uid);

            foreach (var entity in dumpQueue)
            {
                var transform = Transform(entity);
                _transformSystem.SetWorldPositionRotation(entity, targetPos + _random.NextVector2Box() / 4, _random.NextAngle(), transform);
            }

            return;
        }

        if (evt.PlaySound)
        {
            _audio.PlayPredicted(component.DumpSound, uid, args.User);
        }
    }
}
