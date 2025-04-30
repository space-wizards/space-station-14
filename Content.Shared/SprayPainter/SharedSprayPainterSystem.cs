using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Timing;

namespace Content.Shared.SprayPainter;

/// <summary>
/// System for painting paintable objects using a spray painter.
/// Pipes are handled serverside since AtmosPipeColorSystem is server only.
/// </summary>
public abstract class SharedSprayPainterSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedChargesSystem Charges = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public Dictionary<ProtoId<PaintableGroupCategoryPrototype>, PaintableTargets> Targets { get; } = new();

    public override void Initialize()
    {
        base.Initialize();

        CacheStyles();

        SubscribeLocalEvent<SprayPainterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterDoAfterEvent>(OnPaintableDoAfter);
        Subs.BuiEvents<SprayPainterComponent>(SprayPainterUiKey.Key,
            subs =>
            {
                subs.Event<SprayPainterSpritePickedMessage>(OnSpritePicked);
                subs.Event<SprayPainterColorPickedMessage>(OnColorPicked);
                subs.Event<SprayPainterTabChangedMessage>(OnTabChanged);
                subs.Event<SprayPainterDecalPickedMessage>(OnDecalPicked);
                subs.Event<SprayPainterDecalColorPickedMessage>(OnDecalColorPicked);
                subs.Event<SprayPainterDecalAnglePickedMessage>(OnDecalAnglePicked);
            });

        SubscribeLocalEvent<PaintableComponent, InteractUsingEvent>(OnPaintableInteract);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnMapInit(Entity<SprayPainterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ColorPalette.Count == 0)
            return;

        foreach (var target in Targets.Keys.ToList())
        {
            ent.Comp.Indexes[target] = 0;
        }

        SetColor(ent, ent.Comp.ColorPalette.First().Key);
    }

    private void OnPaintableDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!TryComp<PaintableComponent>(target, out var paintableComponent))
            return;

        Appearance.SetData(target, args.Visuals, args.Prototype);
        Audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        Charges.TryUseCharges(new Entity<LimitedChargesComponent?>(ent, EnsureComp<LimitedChargesComponent>(ent)), args.Cost);

        if (TryComp<PaintedComponent>(target, out var paintedComponent))
        {
            paintedComponent.RemoveTime = _timing.CurTime + paintedComponent.RemovalInterval;
            Dirty(target, paintedComponent);
        }
        else
        {
            var comp = AddComp<PaintedComponent>(target);
            comp.RemoveTime = _timing.CurTime + comp.RemovalInterval;
            Dirty(target, comp);
        }


        if (args.Visuals is PaintableVisuals.Canister)
        {
            RaiseLocalEvent(ent,
                new SprayPainterCanisterDoAfterEvent
                {
                    Category = args.Category,
                    Prototype = args.Prototype,
                    DoAfter = args.DoAfter,
                });
        }

        Dirty(target, paintableComponent);

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    #region UI messages

    private void OnTabChanged(EntityUid uid, SprayPainterComponent component, SprayPainterTabChangedMessage args)
    {
        component.SelectedTab = args.Index;
        Dirty(uid, component);
    }

    private void OnColorPicked(Entity<SprayPainterComponent> ent, ref SprayPainterColorPickedMessage args)
    {
        SetColor(ent, args.Key);
    }

    private void OnSpritePicked(Entity<SprayPainterComponent> ent, ref SprayPainterSpritePickedMessage args)
    {
        ent.Comp.Indexes[args.Category] = args.Index;
        Dirty(ent, ent.Comp);
    }

    private void SetColor(Entity<SprayPainterComponent> ent, string? paletteKey)
    {
        if (paletteKey == null || paletteKey == ent.Comp.PickedColor)
            return;

        if (!ent.Comp.ColorPalette.ContainsKey(paletteKey))
            return;

        ent.Comp.PickedColor = paletteKey;
        Dirty(ent, ent.Comp);
    }

    private void OnDecalPicked(EntityUid uid, SprayPainterComponent component, SprayPainterDecalPickedMessage args)
    {
        component.SelectedDecal = args.DecalPrototype;
        Dirty(uid, component);
    }

    private void OnDecalAnglePicked(EntityUid uid,
        SprayPainterComponent component,
        SprayPainterDecalAnglePickedMessage args)
    {
        component.SelectedDecalAngle = args.Angle;
        Dirty(uid, component);
    }

    private void OnDecalColorPicked(EntityUid uid,
        SprayPainterComponent component,
        SprayPainterDecalColorPickedMessage args)
    {
        component.SelectedDecalColor = args.Color;
        Dirty(uid, component);
    }

    #endregion

    private void OnPaintableInteract(Entity<PaintableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SprayPainterComponent>(args.Used, out var painter) ||
            !TryComp<LimitedChargesComponent>(args.Used, out var charges))
            return;

        var group = Proto.Index(ent.Comp.Group);
        var category = Proto.Index(group.Category);

        if (charges.LastCharges <= 0 || charges.LastCharges < category.Cost)
        {
            var msg = Loc.GetString("spray-painter-interact-no-charges");
            _popup.PopupClient(msg, args.User, args.User);
            return;
        }

        if (!Targets.TryGetValue(group.Category, out var target))
            return;

        var selected = painter.Indexes.GetValueOrDefault(group.Category, 0);
        var style = target.Styles[selected];

        if (!group.Styles.TryGetValue(style, out var proto))
        {
            var msg = Loc.GetString("spray-painter-style-not-available");
            _popup.PopupClient(msg, args.User, args.User);
            return;
        }

        var time = target.Time;
        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            time,
            new SprayPainterDoAfterEvent(proto, group.Category, target.Visuals, category.Cost),
            args.Used,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!DoAfter.TryStartDoAfter(doAfterEventArgs, out var id))
            return;

        args.Handled = true;

        // Log the attempt
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} to '{style}' at {Transform(ent).Coordinates:targetlocation}");
    }

    #region Style caching

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<PaintableGroupPrototype>())
            return;

        Targets.Clear();
        CacheStyles();
    }

    protected virtual void CacheStyles()
    {
        foreach (var proto in Proto.EnumeratePrototypes<PaintableGroupPrototype>())
        {
            var targetExists = Targets.ContainsKey(proto.Category);

            SortedSet<string> styles = targetExists
                ? new(Targets[proto.Category].Styles)
                : new();
            var groups = targetExists
                ? Targets[proto.Category].Groups
                : new();

            groups.Add(proto);
            foreach (var style in proto.Styles.Keys)
            {
                styles.Add(style);
            }

            Targets[proto.Category] = new(styles.ToList(), groups, proto.Visuals, proto.Time);
        }
    }

    #endregion
}

/// <summary>
/// Used for convenient cache storage.
/// </summary>
public record PaintableTargets(
    List<string> Styles,
    List<PaintableGroupPrototype> Groups,
    PaintableVisuals Visuals,
    float Time);
