using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.SprayPainter;

/// <summary>
/// System for painting airlocks using a spray painter.
/// Pipes are handled serverside since AtmosPipeColorSystem is server only.
/// </summary>
public abstract class SharedSprayPainterSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private   readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] private   readonly SharedPopupSystem _popup = default!;

    public List<AirlockStyle> Styles { get; private set; } = new();
    public List<AirlockGroupPrototype> Groups { get; private set; } = new();

    [ValidatePrototypeId<AirlockDepartmentsPrototype>]
    private const string Departments = "Departments";

    public override void Initialize()
    {
        base.Initialize();

        CacheStyles();

        SubscribeLocalEvent<SprayPainterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterDoorDoAfterEvent>(OnDoorDoAfter);
        Subs.BuiEvents<SprayPainterComponent>(SprayPainterUiKey.Key, subs =>
        {
            subs.Event<SprayPainterSpritePickedMessage>(OnSpritePicked);
            subs.Event<SprayPainterColorPickedMessage>(OnColorPicked);
        });

        SubscribeLocalEvent<PaintableAirlockComponent, InteractUsingEvent>(OnAirlockInteract);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnMapInit(Entity<SprayPainterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ColorPalette.Count == 0)
            return;

        SetColor(ent, ent.Comp.ColorPalette.First().Key);
    }

    private void OnDoorDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterDoorDoAfterEvent args)
    {
        ent.Comp.AirlockDoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not {} target)
            return;

        if (!TryComp<PaintableAirlockComponent>(target, out var airlock))
            return;

        airlock.Department = args.Department;
        Dirty(target, airlock);

        Audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        Appearance.SetData(target, DoorVisuals.BaseRSI, args.Sprite);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    #region UI messages

    private void OnColorPicked(Entity<SprayPainterComponent> ent, ref SprayPainterColorPickedMessage args)
    {
        SetColor(ent, args.Key);
    }

    private void OnSpritePicked(Entity<SprayPainterComponent> ent, ref SprayPainterSpritePickedMessage args)
    {
        if (args.Index >= Styles.Count)
            return;

        ent.Comp.Index = args.Index;
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

    #endregion

    private void OnAirlockInteract(Entity<PaintableAirlockComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SprayPainterComponent>(args.Used, out var painter) || painter.AirlockDoAfter != null)
            return;

        var group = Proto.Index<AirlockGroupPrototype>(ent.Comp.Group);

        var style = Styles[painter.Index];
        if (!group.StylePaths.TryGetValue(style.Name, out var sprite))
        {
            string msg = Loc.GetString("spray-painter-style-not-available");
            _popup.PopupClient(msg, args.User, args.User);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, painter.AirlockSprayTime, new SprayPainterDoorDoAfterEvent(sprite, style.Department), args.Used, target: ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!DoAfter.TryStartDoAfter(doAfterEventArgs, out var id))
            return;

        // since we are now spraying an airlock prevent spraying more at the same time
        // pipes ignore this
        painter.AirlockDoAfter = id;
        args.Handled = true;

        // Log the attempt
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} to '{style.Name}' at {Transform(ent).Coordinates:targetlocation}");
    }

    #region Style caching

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<AirlockGroupPrototype>() && !args.WasModified<AirlockDepartmentsPrototype>())
            return;

        Styles.Clear();
        Groups.Clear();
        CacheStyles();

        // style index might be invalid now so check them all
        var max = Styles.Count - 1;
        var query = AllEntityQuery<SprayPainterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Index > max)
            {
                comp.Index = max;
                Dirty(uid, comp);
            }
        }
    }

    protected virtual void CacheStyles()
    {
        // collect every style's name
        var names = new SortedSet<string>();
        foreach (var group in Proto.EnumeratePrototypes<AirlockGroupPrototype>())
        {
            Groups.Add(group);
            foreach (var style in group.StylePaths.Keys)
            {
                names.Add(style);
            }
        }

        // get their department ids too for the final style list
        var departments = Proto.Index<AirlockDepartmentsPrototype>(Departments);
        Styles.Capacity = names.Count;
        foreach (var name in names)
        {
            departments.Departments.TryGetValue(name, out var department);
            Styles.Add(new AirlockStyle(name, department));
        }
    }

    #endregion
}

public record struct AirlockStyle(string Name, string? Department);
