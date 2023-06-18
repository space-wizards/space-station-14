// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.PipePainter;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.PipePainter;

/// <summary>
/// A system for painting gas pipes using pipe painter
/// </summary>
public sealed class PipePainterSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AtmosPipeColorSystem _pipeColorSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PipePainterComponent, AfterInteractEvent>(AfterInteractOn);
        SubscribeLocalEvent<PipePainterComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PipePainterComponent, PipePainterSpritePickedMessage>(OnColorPicked);
        SubscribeLocalEvent<PipePainterComponent, PipePainterDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PipePainterComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, PipePainterComponent component, ComponentInit args)
    {
        if (component.ColorPalette.Count == 0)
            return;

        var startColor = component.ColorPalette.First();
        SetColor(uid, component, startColor.Key);
    }

    /// <summary>
    /// Do after painting action is complete - change pipe color & play sound
    /// </summary>
    private void OnDoAfter(EntityUid uid, PipePainterComponent component, PipePainterDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (args.Args.Target is null)
            return;
        var target = (EntityUid) args.Args.Target;

        if(!TryComp<AtmosPipeColorComponent>(target, out var atmosPipeColorComp))
            return;

        _audio.PlayPvs(component.SpraySound, uid);
        _pipeColorSystem.SetColor(target, atmosPipeColorComp, args.Color);
    }

    /// <summary>
    /// When client picks color. Listens to PipePainterSpritePickedMessage.
    /// </summary>
    private void OnColorPicked(EntityUid uid, PipePainterComponent component, PipePainterSpritePickedMessage args)
    {
        SetColor(uid, component, args.Key);
    }

    /// <summary>
    /// Do on activation - open UI
    /// </summary>
    private void OnActivate(EntityUid uid, PipePainterComponent component, ActivateInWorldEvent args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            return;

        UpdateUiState(uid, component);
        component.Owner.GetUIOrNull(PipePainterUiKey.Key)?.Open(actor.PlayerSession);
        args.Handled = true;
    }

    /// <summary>
    /// Do after interaction - start painting a pipe user just interacted with
    /// </summary>
    private void AfterInteractOn(EntityUid uid, PipePainterComponent component, AfterInteractEvent args)
    {
        if(component.PickedColor is null)
            return;

        if (args.Target is not { Valid: true } target || !args.CanReach)
            return;

        if (!EntityManager.HasComponent<AtmosPipeColorComponent>(target))
            return;

        if(!component.ColorPalette.TryGetValue(component.PickedColor, out var color))
            return;

        var doAfterEventArgs = new DoAfterArgs(
            args.User,
            component.SprayTime,
            new PipePainterDoAfterEvent(color),
            uid,
            target,
            uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
            NeedHand = true
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    /// <summary>
    /// Sets a color from component's palette, using a provided palette dictionary key.
    /// </summary>
    private void SetColor(EntityUid uid, PipePainterComponent component, string paletteKey)
    {
        if (!component.ColorPalette.ContainsKey(paletteKey) || paletteKey == component.PickedColor)
            return;

        component.PickedColor = paletteKey;
        UpdateUiState(uid, component);
    }

    private void UpdateUiState(EntityUid uid, PipePainterComponent component)
    {
        PipePainterBoundUserInterfaceState state = new(component.PickedColor, component.ColorPalette);
        _userInterfaceSystem.TrySetUiState(uid, PipePainterUiKey.Key, state);
    }
}
