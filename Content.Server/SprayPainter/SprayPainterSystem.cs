using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.SprayPainter.Prototypes;
using Content.Shared.SprayPainter;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.SprayPainter;

/// <summary>
/// A system for painting airlocks and pipes using enginner painter
/// </summary>
[UsedImplicitly]
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly AtmosPipeColorSystem _pipeColor = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SprayPainterComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<SprayPainterComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterSpritePickedMessage>(OnSpritePicked);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterColorPickedMessage>(OnColorPicked);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterDoorDoAfterEvent>(OnDoorDoAfter);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterPipeDoAfterEvent>(OnPipeDoAfter);
    }

    private void OnInit(EntityUid uid, SprayPainterComponent component, ComponentInit args)
    {
        if (component.ColorPalette.Count == 0)
            return;

        SetColor(uid, component, component.ColorPalette.First().Key);
    }

    private void OnDoorDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterDoorDoAfterEvent args)
    {
        ent.Comp.IsSpraying = false;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not {} target)
            return;

        _audio.PlayPvs(ent.Comp.SpraySound, ent);

        if (!TryComp<PaintableAirlockComponent>(ent, out var airlock))
            return;

        airlock.Department = args.Department;
        _appearance.SetData(target, DoorVisuals.BaseRSI, args.Sprite);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");

        args.Handled = true;
    }

    private void OnPipeDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterPipeDoAfterEvent args)
    {
        ent.Comp.IsSpraying = false;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not {} target)
            return;

        _audio.PlayPvs(ent.Comp.SpraySound, ent);

        if (!TryComp<AtmosPipeColorComponent>(target, out var atmosPipeColor))
            return;

        _pipeColor.SetColor(target, atmosPipeColor, args.Color);

        args.Handled = true;
    }

    private void OnActivate(EntityUid uid, SprayPainterComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        DirtyUI(uid, component);

        _ui.TryOpen(uid, SprayPainterUiKey.Key, actor.PlayerSession);
        args.Handled = true;
    }

    private void AfterInteractOn(EntityUid uid, SprayPainterComponent component, AfterInteractEvent args)
    {
        if (component.IsSpraying || args.Target is not { Valid: true } target || !args.CanReach)
            return;

        if (TryComp<PaintableAirlockComponent>(target, out var airlock))
        {
            if (!_prototypeManager.TryIndex<AirlockGroupPrototype>(airlock.Group, out var grp))
            {
                Log.Error("Group not defined: %s", airlock.Group);
                return;
            }

            var style = Styles[component.Index];
            if (!grp.StylePaths.TryGetValue(style.Name, out var sprite))
            {
                string msg = Loc.GetString("spray-painter-style-not-available");
                _popup.PopupEntity(msg, args.User, args.User);
                return;
            }
            component.IsSpraying = true;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.AirlockSprayTime, new SprayPainterDoorDoAfterEvent(sprite, style.Department), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };
            _doAfter.TryStartDoAfter(doAfterEventArgs);

            // Log attempt
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} is painting {ToPrettyString(uid):target} to '{style.Name}' at {Transform(uid).Coordinates:targetlocation}");
        } else { // Painting pipes
            if (component.PickedColor is null)
                return;

            if (!HasComp<AtmosPipeColorComponent>(target))
                return;

            if (!component.ColorPalette.TryGetValue(component.PickedColor, out var color))
                return;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.PipeSprayTime, new SprayPainterPipeDoAfterEvent(color), uid, target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameTarget,
                NeedHand = true,
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
        }
    }

    private void OnColorPicked(EntityUid uid, SprayPainterComponent component, SprayPainterColorPickedMessage args)
    {
        SetColor(uid, component, args.Key);
    }

    private void OnSpritePicked(EntityUid uid, SprayPainterComponent component, SprayPainterSpritePickedMessage args)
    {
        component.Index = args.Index;
        DirtyUI(uid, component);
    }

    private void SetColor(EntityUid uid, SprayPainterComponent component, string? paletteKey)
    {
        if (paletteKey == null)
            return;

        if (!component.ColorPalette.ContainsKey(paletteKey) || paletteKey == component.PickedColor)
            return;

        component.PickedColor = paletteKey;
        DirtyUI(uid, component);
    }

    private void DirtyUI(EntityUid uid, SprayPainterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _ui.TrySetUiState(
            uid,
            SprayPainterUiKey.Key,
            new SprayPainterBoundUserInterfaceState(
                component.Index,
                component.PickedColor,
                component.ColorPalette));
    }
}
