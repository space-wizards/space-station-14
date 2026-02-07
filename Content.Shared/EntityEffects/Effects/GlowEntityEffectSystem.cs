using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes this entity to glow.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class GlowEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Glow>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSystem = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Glow> args)
    {
        var color = args.Effect.Color;

        if (color == Color.Black)
        {
            // TODO: When we get proper predicted RNG remove this check...
            if (_net.IsClient)
                return;

            color = _random.Pick(Colors);
        }

        var light = _lightSystem.EnsureLight(entity);
        _lightSystem.SetRadius(entity, args.Effect.Radius, light);
        _lightSystem.SetColor(entity, color, light);
        _lightSystem.SetCastShadows(entity, false, light); // this is expensive, and botanists make lots of plants
    }

    public static readonly List<Color> Colors = new()
    {
        Color.White,
        Color.Red,
        Color.Yellow,
        Color.Green,
        Color.Blue,
        Color.Purple,
        Color.Pink
    };
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Glow : EntityEffectBase<Glow>
{
    [DataField]
    public float Radius = 2f;

    [DataField]
    public Color Color = Color.Black;
}
