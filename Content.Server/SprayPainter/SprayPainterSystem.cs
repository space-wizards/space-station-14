using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter;
using Content.Shared.SprayPainter.Components;

namespace Content.Server.SprayPainter;

/// <summary>
/// Handles spraying pipes using a spray painter.
/// Airlocks are handled in shared.
/// </summary>
public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly AtmosPipeColorSystem _pipeColor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SprayPainterComponent, SprayPainterPipeDoAfterEvent>(OnPipeDoAfter);

        SubscribeLocalEvent<AtmosPipeColorComponent, InteractUsingEvent>(OnPipeInteract);
    }

    private void OnPipeDoAfter(Entity<SprayPainterComponent> ent, ref SprayPainterPipeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target is not {} target)
            return;

        if (!TryComp<AtmosPipeColorComponent>(target, out var color))
            return;

        Audio.PlayPvs(ent.Comp.SpraySound, ent);

        _pipeColor.SetColor(target, color, args.Color);

        args.Handled = true;
    }

    private void OnPipeInteract(Entity<AtmosPipeColorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SprayPainterComponent>(args.Used, out var painter) || painter.PickedColor is not {} colorName)
            return;

        if (!painter.ColorPalette.TryGetValue(colorName, out var color))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, painter.PipeSprayTime, new SprayPainterPipeDoAfterEvent(color), args.Used, target: ent, used: args.Used)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            CancelDuplicate = true,
            // multiple pipes can be sprayed at once just not the same one
            DuplicateCondition = DuplicateConditions.SameTarget,
            NeedHand = true
        };

        args.Handled = DoAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
