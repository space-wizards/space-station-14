using System.Numerics;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Charges;
using Content.Server.Decals;
using Content.Server.Destructible;
using Content.Server.Popups;
using Content.Shared.Charges.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;
using Robust.Server.Audio;

namespace Content.Server.SprayPainter;

/// <summary>
/// Handles spraying pipes using a spray painter.
/// Other are handled in shared.
/// </summary>
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly AtmosPipeColorSystem _pipeColor = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChargesSystem _charges = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterComponent, SprayPainterPipeDoAfterEvent>(OnPipeDoAfter);

        SubscribeLocalEvent<AtmosPipeColorComponent, InteractUsingEvent>(OnPipeInteract);

        SubscribeLocalEvent<SprayPainterComponent, SprayPainterCanisterDoAfterEvent>(OnPaintableDoAfter);

        SubscribeLocalEvent<SprayPainterComponent, AfterInteractEvent>(OnFloorAfterInteract);
    }

    private void OnFloorAfterInteract(Entity<SprayPainterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!args.ClickLocation.IsValid(EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("spray-painter-invalid-location"), ent, args.User);
            args.Handled = true;
            return;
        }

        var limitedCharges = Comp<LimitedChargesComponent>(ent);
        if (limitedCharges.LastCharges <= 0)
        {
            _popup.PopupClient(Loc.GetString("spray-painter-interact-no-charges"), args.User, args.User);
            args.Handled = true;
            return;
        }

        if (!ent.Comp.SelectedDecal.HasValue
            || !_decals.TryAddDecal(ent.Comp.SelectedDecal.Value, args.ClickLocation.SnapToGrid(EntityManager).Offset(new(-0.5f)), out _, ent.Comp.SelectedDecalColor, Angle.FromDegrees(ent.Comp.SelectedDecalAngle), 0, true))
        {
            return;
        }

        _audio.PlayPvs(ent.Comp.SpraySound, ent);

        _charges.TryUseCharge(new Entity<LimitedChargesComponent?>(ent, limitedCharges));
        Dirty(ent, limitedCharges);

        _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{EntityManager.ToPrettyString(args.User):user} drew a {ent.Comp.SelectedDecal.Value}");
        args.Handled = true;
    }

    private void OnPaintableDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterCanisterDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled)
            return;

        if (args.Args.Target is not { } target ||
            !TryComp<PaintableComponent>(target, out _))
            return;

        var dummy = Spawn(args.Prototype);

        var destructibleComp = EnsureComp<DestructibleComponent>(dummy);
        CopyComp(dummy, target, destructibleComp);

        Del(dummy);

        args.Handled = true;
    }

    private void OnPipeDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterPipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!TryComp<AtmosPipeColorComponent>(target, out var color))
            return;

        Audio.PlayPvs(ent.Comp.SpraySound, ent);
        _charges.TryUseCharge(new Entity<LimitedChargesComponent?>(ent, EnsureComp<LimitedChargesComponent>(ent)));
        _pipeColor.SetColor(target, color, args.Color);

        args.Handled = true;
    }

    private void OnPipeInteract(Entity<AtmosPipeColorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SprayPainterComponent>(args.Used, out var painter) ||
            !TryComp<LimitedChargesComponent>(args.Used, out var charges) ||
            painter.PickedColor is not { } colorName)
            return;

        if (charges.LastCharges <= 0)
        {
            var msg = Loc.GetString("spray-painter-interact-no-charges");
            _popup.PopupClient(msg, args.User, args.User);
            return;
        }

        if (!painter.ColorPalette.TryGetValue(colorName, out var color))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            painter.PipeSprayTime,
            new SprayPainterPipeDoAfterEvent(color),
            args.Used,
            target: ent,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            // multiple pipes can be sprayed at once just not the same one
            DuplicateCondition = DuplicateConditions.SameTarget,
            NeedHand = true,
        };

        args.Handled = DoAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
