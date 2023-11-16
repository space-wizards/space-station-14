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
using Robust.Shared.Player;

namespace Content.Server.SprayPainter;

/// <summary>
/// A system for painting airlocks and pipes using enginner painter
/// </summary>
[UsedImplicitly]
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AtmosPipeColorSystem _pipeColorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SprayPainterComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<SprayPainterComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterSpritePickedMessage>(OnSpritePicked);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterColorPickedMessage>(OnColorPicked);
        SubscribeLocalEvent<SprayPainterComponent, SprayPainterDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(EntityUid uid, SprayPainterComponent component, ComponentInit args)
    {
        if (component.ColorPalette.Count == 0)
            return;

        SetColor(uid, component, component.ColorPalette.First().Key);
    }

    private void OnDoAfter(EntityUid uid, SprayPainterComponent component, SprayPainterDoAfterEvent args)
    {
        component.IsSpraying = false;

        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        EntityUid target = (EntityUid) args.Args.Target;

        _audio.PlayPvs(component.SpraySound, uid);

        if (TryComp<AtmosPipeColorComponent>(target, out var atmosPipeColorComp))
        {
            _pipeColorSystem.SetColor(target, atmosPipeColorComp, args.Color ?? Color.White);
        } else { // Target is an airlock
            if (args.Sprite != null)
            {
                _appearance.SetData(target, DoorVisuals.BaseRSI, args.Sprite);
                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.Args.User):user} painted {ToPrettyString(args.Args.Target.Value):target}");
            }
        }

        args.Handled = true;
    }

    private void OnActivate(EntityUid uid, SprayPainterComponent component, ActivateInWorldEvent args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;
        DirtyUI(uid, component);

        _userInterfaceSystem.TryOpen(uid, SprayPainterUiKey.Key, actor.PlayerSession);
        args.Handled = true;
    }

    private void AfterInteractOn(EntityUid uid, SprayPainterComponent component, AfterInteractEvent args)
    {
        if (component.IsSpraying || args.Target is not { Valid: true } target || !args.CanReach)
            return;

        if (EntityManager.TryGetComponent<PaintableAirlockComponent>(target, out var airlock))
        {
            if (!_prototypeManager.TryIndex<AirlockGroupPrototype>(airlock.Group, out var grp))
            {
                Log.Error("Group not defined: %s", airlock.Group);
                return;
            }

            string style = Styles[component.Index];
            if (!grp.StylePaths.TryGetValue(style, out var sprite))
            {
                string msg = Loc.GetString("spray-painter-style-not-available");
                _popupSystem.PopupEntity(msg, args.User, args.User);
                return;
            }
            component.IsSpraying = true;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.AirlockSprayTime, new SprayPainterDoAfterEvent(sprite, null), uid, target: target, used: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            };
            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);

            // Log attempt
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):user} is painting {ToPrettyString(uid):target} to '{style}' at {Transform(uid).Coordinates:targetlocation}");
        } else { // Painting pipes
            if(component.PickedColor is null)
                return;

            if (!EntityManager.HasComponent<AtmosPipeColorComponent>(target))
                return;

            if(!component.ColorPalette.TryGetValue(component.PickedColor, out var color))
                return;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.PipeSprayTime, new SprayPainterDoAfterEvent(null, color), uid, target, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                CancelDuplicate = true,
                DuplicateCondition = DuplicateConditions.SameTarget,
                NeedHand = true,
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
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

        _userInterfaceSystem.TrySetUiState(
            uid,
            SprayPainterUiKey.Key,
            new SprayPainterBoundUserInterfaceState(
                component.Index,
                component.PickedColor,
                component.ColorPalette));
    }
}
