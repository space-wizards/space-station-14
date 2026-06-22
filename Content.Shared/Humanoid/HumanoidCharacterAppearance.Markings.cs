using System.Linq;
using System.Numerics;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    private static readonly string SkinColorKey = "skinColor";
    private static readonly string HairColorKey = "hairColor";
    private static readonly string EyeColorKey = "eyeColor";

    /// <summary>
    ///     Creates a new color palette from BaseColor.
    ///     Uses integer provided to choose what kind of palette is generated.
    /// </summary>
    /// <param name="baseColor">The base color to generate a palette from.</param>
    /// <param name="strategy">0 for split complementary, 1 for triadic complementary, any other value for a single complement.</param>
    /// <returns>A list of colours in the chosen palette.</returns>
    /// <remarks>
    ///     Personally I think this should be weighted, but I can't
    ///     be bothered to implement that. -widgetbeck (and mq)
    /// </remarks>
    private static List<Color> GetPaletteFromBase(Color baseColor, int strategy)
    {
        return strategy switch
        {
            0 => GetSplitComplementaries(baseColor),
            1 => GetTriadicComplementaries(baseColor),
            _ => GetOneComplementary(baseColor),
        };
    }

    /// <summary>
    ///     Clamps a 3-toned color palette (skin, hair, eyes) to the desired ISkinColorationStrategy.
    /// </summary>
    /// <returns>
    ///     A 3-toned color palette where:
    ///     0 = Skin colour,
    ///     1 = Hair colour,
    ///     2 = Eye colour.
    /// </returns>
    private static Dictionary<string, Color> ClampPaletteToStrategy(List<Color> colorPalette, SkinColorationPrototype skinType, IRobustRandom random)
    {
        if (colorPalette.Count != 3)
            throw new ArgumentException($"Palettes must have exactly 3 colours, palette contains {colorPalette.Count} colours");

        var newSkinColor = colorPalette[0];
        var newHairColor = colorPalette[1];
        var newEyeColor = colorPalette[2];

        newSkinColor = skinType.Strategy.ClosestSkinColor(newSkinColor);

        if (skinType.RealisticColors)
        {
            // pick a random realistic hair color from the list and randomize it juuuuust a little bit.
            newHairColor = random.Pick(HairStyles.RealisticHairColors);
            newHairColor = newHairColor
                .WithRed(RandomizeColor(newHairColor.R, random))
                .WithGreen(RandomizeColor(newHairColor.G, random))
                .WithBlue(RandomizeColor(newHairColor.B, random));

            // and pick a random realistic eye color from the list.
            newEyeColor = random.Pick(_realisticEyeColors);
        }

        if (skinType.SquashAllColors)
        {
            // crush the other colors down to valid skin colors.
            newHairColor = skinType.Strategy.ClosestSkinColor(newHairColor);
            newEyeColor = skinType.Strategy.ClosestSkinColor(newEyeColor);
        }

        return new Dictionary<string, Color>
        {
            { SkinColorKey, newSkinColor },
            { HairColorKey, newHairColor },
            { EyeColorKey, newEyeColor }
        };
    }

    /// <summary>
    ///     Picks a random marking for a <see cref="HumanoidVisualLayers.Hair"/> or <see cref="HumanoidVisualLayers.FacialHair"/> layer.
    ///     These layers are handled differently to other markings, so we need unique behaviour for them.
    /// </summary>
    /// <returns>A list of markings for the <see cref="HumanoidVisualLayers"/>.</returns>
    private static List<Marking> PickHairsRandomMarking(HumanoidVisualLayers layer, MarkingsLimits layerLimits, IReadOnlyDictionary<string, MarkingPrototype> allMarkings, Color color, IRobustRandom random)
    {
        if (allMarkings.Count == 0 || !random.Prob(layerLimits.Weight))
            return new();

        var hairId = PickWeightedMarkingId(allMarkings, random);
        if (hairId is null || !allMarkings.TryGetValue(hairId, out var hairProto))
            return new();

        if (allMarkings.TryGetValue(hairProto.ID, out var hairMarking))
            return new List<Marking> { hairMarking.AsMarking().WithColor(color) };

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var defaultHair = layer switch
        {
            HumanoidVisualLayers.FacialHair => HairStyles.DefaultFacialHairStyle,
            _ => HairStyles.DefaultHairStyle,
        };

        var defaultHairProto = protoMan.Index(defaultHair);
        return new List<Marking> { new Marking(defaultHair, defaultHairProto.Sprites.Count).WithColor(color) };
    }

    /// <summary>
    ///     Generates a list of random coloured markings for a <see cref="HumanoidVisualLayers"/> layer,
    ///     with respect to the layer and marking weights and marking limits.
    /// </summary>
    /// <param name="allMarkings">A list of all markings for the layer.</param>
    /// <param name="palette">A list of colors to choose from for the markings.</param>
    /// <returns>A list of markings for the desired layer.</returns>
    private static List<Marking> PickLayerRandomMarkings(HumanoidVisualLayers layer, MarkingsLimits? layerLimits, IReadOnlyDictionary<string, MarkingPrototype> allMarkings, Dictionary<string, Color> palette, IRobustRandom random)
    {

        if (layerLimits is null)
            return [];

        if (layer == HumanoidVisualLayers.Hair ||
            layer == HumanoidVisualLayers.FacialHair)
        {
            /* TODO: we should log an error here if using default, but Humanoid is full of static methods so we cant use sawmill until thats fixed
            if (!palette.ContainsKey(HairColorKey))
                sawmill.Error($"Palette for {layer} contains no HairColorKey, using default colour");
            */

            return PickHairsRandomMarking(layer, layerLimits, allMarkings, palette.GetValueOrDefault(HairColorKey), random);
        }

        var layerWeight = layerLimits.Weight;
        var pool = allMarkings.ToDictionary();

        List<Marking> outMarkings = new();

        for (var i = 0; i < layerLimits.Limit; i++)
        {
            // just in case there are somehow more points than markings
            if (pool.Count == 0)
                break;

            // category roll to see if we add anything
            if (!random.Prob(layerWeight))
                continue;

            var randomMarking = PickWeightedMarkingId(pool, random);

            if (randomMarking is null || !pool.Remove(randomMarking, out var protoToAdd))
                continue;

            List<Color> colors = new();
            foreach (var sprite in protoToAdd.Sprites)
            {
                // code here is from MarkingColoring.GetMarkingLayerColors
                // Getting layer name
                var name = sprite switch
                {
                    SpriteSpecifier.Rsi rsi => rsi.RsiState,
                    SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
                    _ => null
                };

                var coloringType = (name == null ||
                    protoToAdd.Coloring.Layers is not { } layers ||
                    !layers.TryGetValue(name, out var layerColoring))
                    ? protoToAdd.Coloring.Default
                    : layerColoring;

                var color = coloringType.Type is not null
                    ? coloringType.GetColor(
                    palette.GetValueOrDefault(SkinColorKey),
                    palette.GetValueOrDefault(EyeColorKey),
                    outMarkings)
                    : random.Pick(new List<Color>
                    {
                        palette.GetValueOrDefault(HairColorKey),
                        palette.GetValueOrDefault(EyeColorKey)
                    });

                colors.Add(color);
            }

            outMarkings.Add(new Marking(protoToAdd, colors));
        }
        return outMarkings;
    }

    /// <summary>
    ///     Uses <see cref="MarkingPrototype"/> weights to pick a random marking from a provided dictionary.
    /// </summary>
    /// <returns>The string ID of the chosen <see cref="MarkingPrototype"/>.</returns>
    private static string? PickWeightedMarkingId(IReadOnlyDictionary<string, MarkingPrototype> markings, IRobustRandom random)
    {
        if (markings.Count == 0)
            return null;

        var weights = markings.ToDictionary(m => m.Key, m => m.Value.RandomWeight);

        return random.Pick(weights).Key;
    }

    #region Color Helpers
    // TODO: These are probably better off in Robust.Shared.Maths.Color

    private static float RandomizeColor(float channel, IRobustRandom random)
    {
        return MathHelper.Clamp01(channel + random.NextFloat(-0.25f, 0.25f));
    }

    /// <summary>
    ///    Generates a complementary colour palette for a provided
    ///    colour by rotating a set amount of degrees around the
    ///    colour wheel, and then varying the value and saturation
    ///    slightly.
    /// </summary>
    /// <returns>
    ///     A list of 3 colors.
    /// </returns>
    private static List<Color> GetComplementaryColors(Color color, float angle)
    {
        var random = IoCManager.Resolve<IRobustRandom>(); // resolving random here so we don't need to pass it into every previous colour method
        var hsl = Color.ToHsl(color);

        // sorry about how messy these are, but to get all random values we need to reroll for positive and negative HSL.
        // since we want to rotate x degrees around the colour wheel, we need to do so in both directions- doing x + x degrees will give us the wrong hue!

        var hVal = hsl.X + angle;
        hVal -= MathF.Floor(hVal);
        var positiveHSL = new Vector4(
            hVal,
            MathHelper.Clamp01(hsl.Y + random.NextFloat(-0.2f, 0f)),
            MathHelper.Clamp01(hsl.Z + random.NextFloat(-0.15f, 0.16f)),
            hsl.W);

        var hVal1 = hsl.X - angle;
        hVal1 += hVal1 <= 0f ? hVal1 + 0.360f : hVal1;
        var negativeHSL = new Vector4(
            hVal1,
            MathHelper.Clamp01(hsl.Y + random.NextFloat(-0.2f, 0f)),
            MathHelper.Clamp01(hsl.Z + random.NextFloat(-0.15f, 0.16f)),
            hsl.W);

        var c0 = Color.FromHsl(positiveHSL);
        var c1 = Color.FromHsl(negativeHSL);

        var palette = new List<Color> { color, c0, c1 };
        return palette;
    }

    /// <summary>
    ///     Generates a list of triadic complementary colors
    /// </summary>
    private static List<Color> GetTriadicComplementaries(Color color)
    {
        return GetComplementaryColors(color, 0.120f);
    }

    /// <summary>
    ///     Generates a list of split complementary colors
    /// </summary>
    private static List<Color> GetSplitComplementaries(Color color)
    {
        return GetComplementaryColors(color, 0.150f);
    }

    /// <summary>
    ///     Generates a list containing the base color and two copies of a single complementary color
    /// </summary>
    private static List<Color> GetOneComplementary(Color color)
    {
        return GetComplementaryColors(color, 0.180f);
    }
    #endregion
}
