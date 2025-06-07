using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SprayPainter.Airlocks.Components;
using Content.Shared.SprayPainter.Airlocks.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SprayPainter.Airlocks;

/// <summary>
/// System for painting airlocks using an entity with the <see cref="Components.AirlockPainterComponent"/>.
/// </summary>
public abstract class SharedAirlockPainterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected readonly List<AirlockStyle> Styles = [];
    protected readonly List<AirlockGroupPrototype> Groups = [];

    [ValidatePrototypeId<AirlockDepartmentsPrototype>]
    private const string Departments = "Departments";

    public override void Initialize()
    {
        base.Initialize();

        CacheStyles();

        SubscribeLocalEvent<AirlockPainterComponent, AirlockPainterDoAfterEvent>(OnDoorDoAfter);
        Subs.BuiEvents<AirlockPainterComponent>(SprayPainterUiKey.Key,
            subs =>
            {
                subs.Event<AirlockPainterSpritePickedMessage>(OnSpritePicked);
            });

        SubscribeLocalEvent<PaintableAirlockComponent, InteractUsingEvent>(OnAirlockInteract);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnDoorDoAfter(Entity<AirlockPainterComponent> ent, ref AirlockPainterDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            args.Args.Target is not { } target ||
            !TryComp<PaintableAirlockComponent>(target, out var airlock))
            return;

        airlock.Department = args.Department;
        Dirty(target, airlock);

        _audio.PlayPredicted(ent.Comp.SpraySound, ent, args.Args.User);
        _appearance.SetData(target, DoorVisuals.BaseRSI, args.Sprite.ToString());
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    private void OnSpritePicked(Entity<AirlockPainterComponent> ent, ref AirlockPainterSpritePickedMessage args)
    {
        if (args.Index >= Styles.Count)
            return;

        ent.Comp.Index = args.Index;
        Dirty(ent, ent.Comp);
    }

    private void OnAirlockInteract(Entity<PaintableAirlockComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<AirlockPainterComponent>(args.Used, out var painter))
            return;

        var group = _proto.Index(ent.Comp.Group);

        var style = Styles[painter.Index];
        if (!group.StylePaths.TryGetValue(style.Name, out var sprite))
        {
            var msg = Loc.GetString("spray-painter-style-not-available");
            _popup.PopupClient(msg, args.User, args.User);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            painter.AirlockSprayTime,
            new AirlockPainterDoAfterEvent(sprite, style.Department),
            args.Used,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        if (!_doAfter.TryStartDoAfter(doAfterEventArgs, out _))
            return;

        args.Handled = true;

        // Log the attempt
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.User):user} is painting {ToPrettyString(ent):target} to '{style.Name}' at {Transform(ent).Coordinates:targetlocation}");
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
        var query = AllEntityQuery<AirlockPainterComponent>();
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
        foreach (var group in _proto.EnumeratePrototypes<AirlockGroupPrototype>())
        {
            Groups.Add(group);
            foreach (var style in group.StylePaths.Keys)
            {
                names.Add(style);
            }
        }

        // get their department ids too for the final style list
        var departments = _proto.Index<AirlockDepartmentsPrototype>(Departments);
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
