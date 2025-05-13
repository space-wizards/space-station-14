using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.SprayPainter;

/// <summary>
/// System for painting paintable objects using a spray painter.
/// Pipes are handled serverside since AtmosPipeColorSystem is server only.
/// </summary>
public abstract class SharedSprayPainterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedChargesSystem Charges = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public Dictionary<ProtoId<PaintableGroupCategoryPrototype>, PaintableTargets> Targets { get; } = new();

    public override void Initialize()
    {
        base.Initialize();

        CacheStyles();

        SubscribeLocalEvent<SprayPainterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterDoAfterEvent>(OnPaintableDoAfter);
        SubscribeLocalEvent<SprayPainterComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);
        Subs.BuiEvents<SprayPainterComponent>(SprayPainterUiKey.Key,
            subs =>
            {
                subs.Event<SprayPainterSetCategoryPrototypeMessage>(OnSpritePicked);
                subs.Event<SprayPainterSetPipeColorMessage>(OnColorPicked);
                subs.Event<SprayPainterTabChangedMessage>(OnTabChanged);
                subs.Event<SprayPainterSetDecalMessage>(OnDecalPicked);
                subs.Event<SprayPainterSetDecalColorMessage>(OnDecalColorPicked);
                subs.Event<SprayPainterSetDecalAngleMessage>(OnDecalAnglePicked);
            });

        SubscribeLocalEvent<PaintableComponent, InteractUsingEvent>(OnPaintableInteract);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnMapInit(Entity<SprayPainterComponent> ent, ref MapInitEvent args)
    {
        foreach (var target in Targets.Keys.ToList())
            ent.Comp.Indexes[target] = 0;

        if (ent.Comp.ColorPalette.Count > 0)
            SetColor(ent, ent.Comp.ColorPalette.First().Key);
    }

    private void OnPaintableDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!HasComp<PaintableComponent>(target))
            return;

        Appearance.SetData(target, args.Visuals, args.Prototype);
        Audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        Charges.TryUseCharges(new Entity<LimitedChargesComponent?>(ent, EnsureComp<LimitedChargesComponent>(ent)), args.Cost);

        var paintedComponent = EnsureComp<PaintedComponent>(target);
        paintedComponent.RemoveTime = _timing.CurTime + ent.Comp.FreshPaintDuration;
        Dirty(target, paintedComponent);

        RaiseLocalEvent(target,
            new EntityPaintedEvent
            {
                User = args.User,
                Tool = ent,
                Prototype = args.Prototype,
                Category = args.Category
            });

        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    private void OnAddSwitchModeVerb(Entity<SprayPainterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue)
            return;

        var verbLocString = ent.Comp.IsPaintingDecals
            ? "spray-painter-verb-enable-decals"
            : "spray-painter-verb-disable-decals";
        var user = args.User;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString(verbLocString),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => TogglePaintDecals(ent, user),
            Impact = LogImpact.Low
        };
        args.Verbs.Add(verb);
    }

    private void TogglePaintDecals(Entity<SprayPainterComponent> ent, EntityUid user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        ent.Comp.IsPaintingDecals = !ent.Comp.IsPaintingDecals;
        Dirty(ent);

        // Make the machine beep.
        var pitch = ent.Comp.IsPaintingDecals ? 1 : 0.8f;
        Audio.PlayPredicted(ent.Comp.SoundSwitchMode, ent, user, ent.Comp.SoundSwitchMode.Params.WithPitchScale(pitch));
    }

    #region UI messages

    private void OnTabChanged(Entity<SprayPainterComponent> ent, ref SprayPainterTabChangedMessage args)
    {
        ent.Comp.SelectedTab = args.Index;
        Dirty(ent);
    }

    private void OnColorPicked(Entity<SprayPainterComponent> ent, ref SprayPainterSetPipeColorMessage args)
    {
        SetColor(ent, args.Key);
    }

    private void OnSpritePicked(Entity<SprayPainterComponent> ent, ref SprayPainterSetCategoryPrototypeMessage args)
    {
        if (!ent.Comp.Indexes.ContainsKey(args.Category))
            return;

        ent.Comp.Indexes[args.Category] = args.Index;
        Dirty(ent);
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

    private void OnDecalPicked(EntityUid uid, SprayPainterComponent component, SprayPainterSetDecalMessage args)
    {
        component.SelectedDecal = args.DecalPrototype;
        Dirty(uid, component);
    }

    private void OnDecalAnglePicked(EntityUid uid,
        SprayPainterComponent component,
        SprayPainterSetDecalAngleMessage args)
    {
        component.SelectedDecalAngle = args.Angle;
        Dirty(uid, component);
    }

    private void OnDecalColorPicked(EntityUid uid,
        SprayPainterComponent component,
        SprayPainterSetDecalColorMessage args)
    {
        component.SelectedDecalColor = args.Color;
        Dirty(uid, component);
    }

    #endregion

    private void OnPaintableInteract(Entity<PaintableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SprayPainterComponent>(args.Used, out var painter)
            || !TryComp<LimitedChargesComponent>(args.Used, out var charges))
            return;

        if (ent.Comp.Group == null
            || !Proto.TryIndex(ent.Comp.Group, out var group)
            || !Proto.TryIndex(group.Category, out var category))
            return;

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

        if (!DoAfter.TryStartDoAfter(doAfterEventArgs, out _))
            return;

        args.Handled = true;

        // Log the attempt
        AdminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} to '{style}' at {Transform(ent).Coordinates:targetlocation}");
    }

    #region Style caching

    protected virtual void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
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
