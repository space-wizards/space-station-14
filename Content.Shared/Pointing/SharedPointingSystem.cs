using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.Ghost;
using Content.Shared.CCVar;
using Content.Shared.Input;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Pointing.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Pointing;

public abstract partial class SharedPointingSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] protected ExamineSystemShared Examine = default!;
    [Dependency] protected IGameTiming GameTiming = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] protected RotateToFaceSystem RotateToFace = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedVisibilitySystem Visibility = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;
    [Dependency] private ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private SharedMapSystem _map = default!;

    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;

    protected readonly TimeSpan PointDuration = TimeSpan.FromSeconds(4);
    protected readonly float PointKeyTimeMove = 0.1f;
    protected readonly float PointKeyTimeHover = 0.5f;

    private const float PointingRange = 15f;
    private TimeSpan _pointDelay = TimeSpan.FromSeconds(0.5f);

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Point, new PointerInputCmdHandler(HandlePointInput))
            .Register<SharedPointingSystem>();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddPointingVerb);
        Subs.CVar(_config, CCVars.PointingCooldownSeconds, v => _pointDelay = TimeSpan.FromSeconds(v), true);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PointingArrowComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.Rogue || component.EndTime > GameTiming.CurTime)
                continue;

            RemCompDeferred<PointingArrowComponent>(uid);
            EnsureComp<RoguePointingArrowComponent>(uid);
            EnsureComp<TimedDespawnComponent>(uid).Lifetime = 10f;
        }
    }

    public bool TryPoint(ICommonSession? session, EntityCoordinates coordsPointed, EntityUid pointed)
    {
        if (session?.AttachedEntity is not { } player)
        {
            Log.Warning($"Player {session} attempted to point without any attached entity");
            return false;
        }

        return TryPoint(player, coordsPointed, pointed);
    }

    public bool TryPoint(EntityUid pointer, EntityCoordinates coordsPointed, EntityUid pointed)
    {
        if (!coordsPointed.IsValid(EntityManager))
        {
            Log.Warning($"Player {ToPrettyString(pointer)} attempted to point at invalid coordinates: {coordsPointed}");
            return false;
        }

        var pointerUser = EnsureComp<PointerUserComponent>(pointer);
        if (GameTiming.CurTime < pointerUser.NextPointTime)
        {
            return false;
        }

        if (!CanPoint(pointer, coordsPointed, pointed))
        {
            if (!InRange(pointer, coordsPointed))
                Popup.PopupPredicted(Loc.GetString("pointing-system-try-point-cannot-reach"), pointer, pointer, Filter.Empty(), false);

            return false;
        }

        Point(pointer, coordsPointed, pointed);
        pointerUser.NextPointTime = GameTiming.CurTime + _pointDelay;
        Dirty(pointer, pointerUser);
        return true;
    }

    private bool HandlePointInput(ICommonSession? session, EntityCoordinates coordsPointed, EntityUid pointed)
    {
        // Yes we want the opposite, false means server will get it, true means they won't.
        return !TryPoint(session, coordsPointed, pointed);
    }

    public bool CanPoint(EntityUid pointer, EntityCoordinates coordinates, EntityUid pointed)
    {
        if (!Exists(pointer) || !coordinates.IsValid(EntityManager))
            return false;

        if (IsPointingArrow(pointed))
            return false;

        var ev = new PointAttemptEvent(pointer);
        RaiseLocalEvent(pointer, ref ev);

        return !ev.Cancelled && InRange(pointer, coordinates);
    }

    public EntityUid Point(EntityUid pointer, EntityCoordinates coordinates, EntityUid pointed)
    {
        var mapCoordinates = TransformSystem.ToMapCoordinates(coordinates);
        BeforePoint(pointer, mapCoordinates);

        // DO NOT FLAG AS PREDICTED
        // We can't predict animations properly and do not need the entity to be accurate.
        var arrow = EntityManager.CreateEntityUninitialized("PointingArrow", coordinates);

        var pointing = Comp<PointingArrowComponent>(arrow);
        pointing.StartPosition = TransformSystem
            .ToCoordinates((arrow, Transform(arrow)), TransformSystem.ToMapCoordinates(Transform(pointer).Coordinates))
            .Position;
        pointing.EndTime = GameTiming.CurTime + PointDuration;
        pointing.Rogue = ShouldPointingArrowGoRogue();
        Dirty(arrow, pointing);
        pointing.Owner = pointer;

        ConfigureArrow(pointer, arrow, pointing);
        EntityManager.InitializeAndStartEntity(arrow);
        AfterPoint(pointer, mapCoordinates, pointed);

        return arrow;
    }

    public bool InRange(EntityUid pointer, EntityCoordinates coordinates)
    {
        if (HasComp<GhostComponent>(pointer))
            return TransformSystem.InRange(Transform(pointer).Coordinates, coordinates, PointingRange);

        return Examine.InRangeUnOccluded(pointer, coordinates, PointingRange, predicate: e => e == pointer);
    }

    protected void BeforePoint(EntityUid pointer, MapCoordinates coordinates)
    {
        RotateToFace.TryFaceCoordinates(pointer, coordinates.Position);
    }

    protected void ConfigureArrow(EntityUid pointer, EntityUid arrow, PointingArrowComponent component)
    {
        if (component.Rogue)
            return;

        EnsureComp<TimedDespawnComponent>(arrow).Lifetime = (float) PointDuration.TotalSeconds;

        if (TryComp(pointer, out VisibilityComponent? playerVisibility))
        {
            var arrowVisibility = EnsureComp<VisibilityComponent>(arrow);
            Visibility.SetLayer((arrow, arrowVisibility), playerVisibility.Layer);
        }
    }

    protected virtual void AfterPoint(
        EntityUid pointer,
        MapCoordinates mapCoordinates,
        EntityUid pointed)
    {
        var (pointedMessageTarget, selfMessage, othersMessage, targetMessage) = GetPointingMessages(pointer, pointed, mapCoordinates);
        var selfPopupType = pointer == pointedMessageTarget ? PopupType.Medium : PopupType.Small;
        Popup.PopupPredicted(selfMessage, pointer, pointer, Filter.Empty(), false, selfPopupType);

        var othersFilter = Filter.PvsExcept(pointer, entityManager: EntityManager);
        if (Exists(pointedMessageTarget) && pointedMessageTarget != pointer)
        {
            if (targetMessage != null)
                Popup.PopupEntity(targetMessage, pointer, pointedMessageTarget, PopupType.Medium);

            othersFilter = othersFilter.RemovePlayerByAttachedEntity(pointedMessageTarget);
        }

        Popup.PopupPredicted(othersMessage, pointer, null, othersFilter, true);

        if (Exists(pointedMessageTarget))
        {
            var ev = new AfterPointedAtEvent(pointedMessageTarget);
            RaiseLocalEvent(pointer, ref ev);
            var gotEv = new AfterGotPointedAtEvent(pointer);
            RaiseLocalEvent(pointedMessageTarget, ref gotEv);
        }
    }

    protected virtual bool ShouldPointingArrowGoRogue()
    {
        return false;
    }

    private bool IsPointingArrow(EntityUid uid)
    {
        return HasComp<PointingArrowComponent>(uid);
    }

    private (EntityUid Pointed, string Self, string Others, string? Target) GetPointingMessages(
        EntityUid pointer,
        EntityUid pointed,
        MapCoordinates mapCoordinates)
    {
        var pointerName = Identity.Entity(pointer, EntityManager);

        if (!Exists(pointed))
        {
            var tileName = GetTileName(mapCoordinates);

            return (pointed,
                Loc.GetString("pointing-system-point-at-tile", ("tileName", tileName)),
                Loc.GetString("pointing-system-other-point-at-tile", ("otherName", pointerName), ("tileName", tileName)),
                null);
        }

        var effectivePointed = GetPointingTarget(pointed);
        var pointedName = Identity.Entity(effectivePointed, EntityManager);
        var pointingAtSelf = pointer == effectivePointed;

        if (effectivePointed != pointed)
        {
            var itemName = Identity.Entity(pointed, EntityManager);

            if (pointingAtSelf)
            {
                return (effectivePointed,
                    Loc.GetString("pointing-system-point-in-own-inventory-self", ("item", itemName)),
                    Loc.GetString("pointing-system-point-in-own-inventory-others", ("item", itemName), ("pointer", pointerName)),
                    null);
            }

            return (effectivePointed,
                Loc.GetString("pointing-system-point-in-other-inventory-self", ("item", itemName), ("wearer", pointedName)),
                Loc.GetString("pointing-system-point-in-other-inventory-others", ("item", itemName), ("pointer", pointerName), ("wearer", pointedName)),
                Loc.GetString("pointing-system-point-in-other-inventory-target", ("item", itemName), ("pointer", pointerName)));
        }

        return (effectivePointed,
            pointingAtSelf
                ? Loc.GetString("pointing-system-point-at-self")
                : Loc.GetString("pointing-system-point-at-other", ("other", pointedName)),
            pointingAtSelf
                ? Loc.GetString("pointing-system-point-at-self-others", ("otherName", pointerName), ("other", pointerName))
                : Loc.GetString("pointing-system-point-at-other-others", ("otherName", pointerName), ("other", pointedName)),
            pointingAtSelf ? null : Loc.GetString("pointing-system-point-at-you-other", ("otherName", pointerName)));
    }

    protected EntityUid GetPointingTarget(EntityUid pointed)
    {
        foreach (var container in _container.GetContainingContainers(pointed))
        {
            if (_inventoryQuery.HasComp(container.Owner))
                return container.Owner;
        }

        return pointed;
    }

    protected string GetTileName(MapCoordinates mapCoordinates)
    {
        return Loc.GetString(_tileDefinitionManager[GetTile(mapCoordinates)?.TypeId ?? 0].Name);
    }

    protected string GetTileLogPosition(MapCoordinates mapCoordinates)
    {
        if (!_mapManager.TryFindGridAt(mapCoordinates, out var gridUid, out var grid))
            return mapCoordinates.ToString();

        return $"EntId={gridUid} {_map.WorldToTile(gridUid, grid, mapCoordinates.Position)}";
    }

    private Tile? GetTile(MapCoordinates mapCoordinates)
    {
        if (!_mapManager.TryFindGridAt(mapCoordinates, out var gridUid, out var grid))
            return null;

        return _map.GetTileRef(gridUid, grid, _map.WorldToTile(gridUid, grid, mapCoordinates.Position)).Tile;
    }

    private void AddPointingVerb(GetVerbsEvent<Verb> args)
    {
        // Load-bearing see predicted entity comment
        if (!GameTiming.IsFirstTimePredicted)
            return;

        if (IsClientSide(args.Target))
            return;

        if (IsPointingArrow(args.Target))
            return;

        if (!CanPoint(args.User, Transform(args.Target).Coordinates, args.Target))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("pointing-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/point.svg.192dpi.png")),
            Act = () => TryPoint(args.User, Transform(args.Target).Coordinates, args.Target)
        };

        args.Verbs.Add(verb);
    }
}

[ByRefEvent]
public record struct PointAttemptEvent(EntityUid Uid)
{
    public bool Cancelled;

    public void Cancel()
    {
        Cancelled = true;
    }
}
