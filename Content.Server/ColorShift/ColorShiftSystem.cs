using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.ColorShift;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.ColorShift;

/// <summary>
/// This handles the special ability for slimes to color-shift to different colors.
/// </summary>
public sealed class ColorShiftSystem : SharedColorShiftSystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ColorShifterComponent, PleaseHueShiftNetworkMessage>(OnMessageReceive);
        SubscribeLocalEvent<HumanoidAppearanceComponent, HueShiftDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<HumanoidAppearanceComponent> ent, ref HueShiftDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var color = args.NewColor;
        var colorList = new List<Color> { color }; // needed for markings
        _appearanceSystem.SetSkinColor(ent.Owner, color);
        // Hardcoding it to zero is a bandaid until someone can figure out how to loop through all possible hair marking slots and set the color there
        // Leaving that as an exercise for the reader :P
        _appearanceSystem.SetMarkingColor(ent.Owner, MarkingCategories.Hair, 0, colorList);
        _appearanceSystem.SetMarkingColor(ent.Owner, MarkingCategories.FacialHair, 0, colorList);
    }

    private void OnMessageReceive(Entity<ColorShifterComponent> ent, ref PleaseHueShiftNetworkMessage args)
    {
        Log.Debug(args.Actor.Id.ToString());

        // Validate client input
        if (ent.Owner != _entityManager.GetEntity(args.Entity))
            return;

        // Perform doafter
        var evt = new HueShiftDoAfterEvent(Color.FromHsv(new Vector4(args.Hue, args.Saturation, args.Value, 1f)));
        var doAfterArgs = new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.HueShiftLength, evt, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }
}
