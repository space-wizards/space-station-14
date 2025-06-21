using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SprayPainter.AtmosPipes;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SprayPainter.AtmosPipes;

/// <summary>
/// The server handles actually changing the appearance of pipes.
/// </summary>
public sealed class AtmosPipePainterSystem : SharedAtmosPipePainterSystem
{
    [Dependency] private readonly AtmosPipeColorSystem _pipeColor = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipePainterComponent, AtmosPipePainterDoAfterEvent>(OnPipeDoAfter);
        SubscribeLocalEvent<AtmosPipeColorComponent, InteractUsingEvent>(OnPipeInteract);
    }

    private void OnPipeDoAfter(Entity<AtmosPipePainterComponent> ent, ref AtmosPipePainterDoAfterEvent args)
    {
        if (args.Handled ||
            args.Cancelled ||
            args.Args.Target is not { } target ||
            !TryComp<AtmosPipeColorComponent>(target, out var color))
            return;

        _audio.PlayPvs(ent.Comp.SpraySound, ent);
        _pipeColor.SetColor(target, color, args.Color);

        args.Handled = true;
    }

    private void OnPipeInteract(Entity<AtmosPipeColorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<AtmosPipePainterComponent>(args.Used, out var painter) ||
            painter.PickedColor is not { } colorName ||
            !painter.ColorPalette.TryGetValue(colorName, out var color))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            args.User,
            painter.PipeSprayTime,
            new AtmosPipePainterDoAfterEvent(color),
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

        args.Handled = _doAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
