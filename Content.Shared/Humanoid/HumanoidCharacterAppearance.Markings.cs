using System.Linq;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using static Content.Shared.Preferences.HumanoidCharacterProfile;

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
    private static Color[] GetPaletteFromBase(Color baseColor, int strategy)
    {
        return strategy switch
        {
            0 => baseColor.GetSplitComplementaries(),
            1 => baseColor.GetTriadicComplementaries(),
            _ => baseColor.GetOneComplementary(),
        };
    }

    /// <summary>
    ///     Clamps a 3-toned color palette (in the order of skin, hair, eyes) to the desired ISkinColorationStrategy.
    /// </summary>
    /// <remarks>
    ///     Optionally accepts <see cref="RandomizeCfg"/> and a base <see cref="HumanoidCharacterAppearance"/>
    ///     to retain values of an existing appearance.
    /// </remarks>
    /// <returns>
    ///     A 3-toned color palette with keys skinColor, hairColor, and eyeColor.
    /// </returns>
    private static Dictionary<string, Color> ClampPaletteToStrategy(Color[] colorPalette, SkinColorationPrototype skinType, IRobustRandom random, RandomizeCfg? charEditorRandomizeConfig, HumanoidCharacterAppearance? baseAppearance)
    {
        if (colorPalette.Length != 3)
            throw new ArgumentException($"Palettes must have exactly 3 colours, palette contains {colorPalette.Length} colours");

        var newSkinColor = (charEditorRandomizeConfig & RandomizeCfg.Skin) != 0 || baseAppearance is null
            ? colorPalette[0] : baseAppearance.SkinColor;
        var newHairColor = colorPalette[1];
        var newEyeColor = (charEditorRandomizeConfig & RandomizeCfg.Eyes) != 0 || baseAppearance is null
            ? colorPalette[2] : baseAppearance.EyeColor;

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

    private static float RandomizeColor(float channel, IRobustRandom random)
    {
        return MathHelper.Clamp01(channel + random.NextFloat(-0.25f, 0.25f));
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
}
