using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Charges;
using Content.Server.Decals;
using Content.Server.Destructible;
using Content.Server.Popups;
using Content.Shared.Atmos.Piping.Unary.Components;
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
/// Handles spraying pipes and decals using a spray painter.
/// Other paintable objects are handled in shared.
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
        SubscribeLocalEvent<SprayPainterComponent, AfterInteractEvent>(OnFloorAfterInteract);
        SubscribeLocalEvent<AtmosPipeColorComponent, InteractUsingEvent>(OnPipeInteract);
        SubscribeLocalEvent<GasCanisterComponent, EntityPaintedEvent>(OnCanisterPainted);
    }

    /// <summary>
    /// Handles drawing decals when a spray painter is used to interact with the floor.
    /// </summary>
    private void OnFloorAfterInteract(Entity<SprayPainterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (!ent.Comp.IsPaintingDecals)
            return;

        if (TryComp(ent, out LimitedChargesComponent? charges) && charges.LastCharges < ent.Comp.DecalChargeCost)
        {
            _popup.PopupEntity(Loc.GetString("spray-painter-interact-no-charges"), args.User, args.User);
            args.Handled = true;
            return;
        }

        var position = args.ClickLocation;
        if (ent.Comp.SnapDecals)
            position = position.SnapToGrid(EntityManager).Offset(new(-0.5f));
        else
            position = position.Offset(new(-0.5f, -0.5f));

        if (!_decals.TryAddDecal(ent.Comp.SelectedDecal, position, out _, ent.Comp.SelectedDecalColor, Angle.FromDegrees(ent.Comp.SelectedDecalAngle), 0, false))
            return;

        _audio.PlayPvs(ent.Comp.SpraySound, ent);

        _charges.TryUseCharges((ent, charges), ent.Comp.DecalChargeCost);

        AdminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{EntityManager.ToPrettyString(args.User):user} drew a {ent.Comp.SelectedDecal}");
        args.Handled = true;
    }

    /// <summary>
    /// Event handler when gas canisters are painted.
    /// The canister's color should not change when it's destroyed.
    /// </summary>
    private void OnCanisterPainted(Entity<GasCanisterComponent> ent, ref EntityPaintedEvent args)
    {
        var dummy = Spawn(args.Prototype);

        var destructibleComp = EnsureComp<DestructibleComponent>(dummy);
        CopyComp(dummy, ent, destructibleComp);

        Del(dummy);
    }

    private void OnPipeDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterPipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!TryComp<AtmosPipeColorComponent>(target, out var color))
            return;

        if (TryComp<LimitedChargesComponent>(ent, out var charges) &&
            !_charges.TryUseCharges((ent, charges), ent.Comp.PipeChargeCost))
            return;

        Audio.PlayPvs(ent.Comp.SpraySound, ent);
        _pipeColor.SetColor(target, color, args.Color);

        args.Handled = true;
    }

    private void OnPipeInteract(Entity<AtmosPipeColorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SprayPainterComponent>(args.Used, out var painter) ||
            painter.PickedColor is not { } colorName)
            return;

        if (!painter.ColorPalette.TryGetValue(colorName, out var color))
            return;

        if (TryComp<LimitedChargesComponent>(args.Used, out var charges)
            && charges.LastCharges < painter.PipeChargeCost)
        {
            var msg = Loc.GetString("spray-painter-interact-no-charges");
            _popup.PopupEntity(msg, args.User, args.User);
            return;
        }

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
