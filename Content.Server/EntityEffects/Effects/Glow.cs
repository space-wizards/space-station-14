using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Makes a mob glow.
/// </summary>
public sealed partial class Glow : EntityEffect
{
    [DataField]
    public float Radius = 2f;

    [DataField]
    public Color Color = Color.Black;

    private static readonly List<Color> Colors = new()
    {
        Color.White,
        Color.Red,
        Color.Yellow,
        Color.Green,
        Color.Blue,
        Color.Purple,
        Color.Pink
    };

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (Color == Color.Black)
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            Color = random.Pick(Colors);
        }

        var lightSystem = args.EntityManager.System<SharedPointLightSystem>();
        var light = lightSystem.EnsureLight(args.TargetEntity);
        lightSystem.SetRadius(args.TargetEntity, Radius, light);
        lightSystem.SetColor(args.TargetEntity, Color, light);
        lightSystem.SetCastShadows(args.TargetEntity, false, light); // this is expensive, and botanists make lots of plants
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
