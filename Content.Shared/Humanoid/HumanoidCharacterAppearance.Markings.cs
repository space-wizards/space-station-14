using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    /// <summary>
    ///     Stores 3 colours as character skin tone, hair tone, and eye tone.
    /// </summary>
    private record CharacterPalette(Color SkinColor, Color HairColor, Color EyeColor);

    # region Palettes

    /// <summary>
    ///     Generates a new random <see cref="CharacterPalette"/>, clamped to a <see cref="SkinColorationPrototype"/> strategy.
    /// </summary>
    private static CharacterPalette GetRandomClampedPalette(SkinColorationPrototype skinColoration, IRobustRandom random)
    {
        var baseColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);
        var palette = GetPaletteFromBase(baseColor, random.Next(4));
        return ClampPaletteToStrategy(palette, skinColoration, random);
    }

    /// <summary>
    ///     Creates a new color palette from BaseColor.
    ///     Uses integer provided to choose what kind of palette is generated.
    /// </summary>
    /// <param name="baseColor">The base color to generate a palette from.</param>
    /// <param name="strategy">0 for split complementary, 1 for triadic complementary, 2 for analogous, any other value for a single complement.</param>
    /// <returns>A list of colours in the chosen palette.</returns>
    /// <remarks>
    ///     Personally I think this should be weighted, but I can't
    ///     be bothered to implement that. -widgetbeck (and mq)
    /// </remarks>
    private static CharacterPalette GetPaletteFromBase(Color baseColor, int strategy)
    {
        var list = strategy switch
        {
            0 => baseColor.GetSplitComplementaries(),
            1 => baseColor.GetTriadicComplementaries(),
            2 => baseColor.GetAnalogousComplementaries(),
            _ => baseColor.GetOneComplementary(),
        };

        return new(list[0], list[1], list[2]);
    }

    /// <summary>
    ///     Clamps a <see cref="CharacterPalette"/> to the desired ISkinColorationStrategy.
    /// </summary>
    private static CharacterPalette ClampPaletteToStrategy(CharacterPalette palette, SkinColorationPrototype skinType, IRobustRandom random)
    {
        palette = palette with
        {
            SkinColor = skinType.Strategy.EnsureVerified(palette.SkinColor),
            HairColor = ClampHairColorToStrategy(palette.HairColor, skinType, random),
            EyeColor = ClampEyeColorToStrategy(palette.EyeColor, skinType)
        };

        return palette;
    }

    /// <summary>
    ///     Clamps a hair color to a <see cref="SkinColorationPrototype"/> strategy.
    /// </summary>
    private static Color ClampHairColorToStrategy(Color color, SkinColorationPrototype skinType, IRobustRandom random)

    {
        if (skinType.RealisticColors)
        {
            static float RandomizeColor(float channel, IRobustRandom random) =>
                MathHelper.Clamp01(channel + random.NextFloat(-0.25f, 0.25f));

            // pick a random realistic hair color from the list and randomize it juuuuust a little bit.
            color = random.Pick(HairStyles.RealisticHairColors);
            color = color
                .WithRed(RandomizeColor(color.R, random))
                .WithGreen(RandomizeColor(color.G, random))
                .WithBlue(RandomizeColor(color.B, random));
        }

        if (skinType.SquashEyeHairColors)
            color = skinType.Strategy.ClosestSkinColor(color);

        return color;
    }

    /// <summary>
    ///     Clamps an eye color to a <see cref="SkinColorationPrototype"/> strategy.
    /// </summary>

    private static Color ClampEyeColorToStrategy(Color color, SkinColorationPrototype skinType)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        if (skinType.RealisticColors && !RealisticEyeColors.Contains(color))
            color = random.Pick(RealisticEyeColors);

        if (skinType.SquashEyeHairColors)
            color = skinType.Strategy.EnsureVerified(color);

        return color;
    }

    #endregion
    #region Markings

    // TODO refactor how markings work completely so this isnt such a behemoth

    /// <summary>
    ///     Generates random colored markings for a specified character, respecting species and sex.
    /// </summary>
    /// <param name="species">Species of the character.</param>
    /// <param name="sex">Sex of the character.</param>
    /// <param name="palette">Palette used to color markings.</param>
    /// <returns>A dictionary of organs to their corresponding visual layers, and the markings corresponding to those visual layers.</returns>
    private static Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> RandomizeMarkings(SpeciesPrototype species, Sex sex, CharacterPalette palette, IPrototypeManager protoMan, IRobustRandom random)
    {
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var markingData = markingManager.GetMarkingData(species);

        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings = new();

        foreach (var (organ, organData) in markingData)
        {
            // if this is an organ with no markings (heart, stomach, etc)
            if (!protoMan.TryIndex(organData.Group, out var groupProto))
                continue;

            Dictionary<HumanoidVisualLayers, List<Marking>> layerMarkings = new();
            foreach (var layer in organData.Layers)
            {
                var allMarkings = markingManager.MarkingsByLayerAndGroupAndSex(layer, organData.Group, sex);

                if (allMarkings.Count == 0)
                    continue;

                var layerLimits = groupProto.Limits.GetValueOrDefault(layer);
                if (layerLimits is null || layerLimits.Limit <= 0)
                    continue;

                layerMarkings.Add(layer, PickLayerRandomMarkings(layer, layerLimits, allMarkings, palette, random));
            }
            markings.Add(organ, layerMarkings);
        }
        return markings;
    }

    /// <summary>
    ///     Generates a list of random coloured markings for a <see cref="HumanoidVisualLayers"/> layer,
    ///     with respect to the layer and marking weights and marking limits.
    /// </summary>
    /// <param name="allMarkings">A list of all markings for the layer.</param>
    /// <param name="palette">A list of colors to choose from for the markings.</param>
    /// <returns>A list of markings for the desired layer.</returns>
    private static List<Marking> PickLayerRandomMarkings(HumanoidVisualLayers layer, MarkingsLimits? layerLimits, IReadOnlyDictionary<string, MarkingPrototype> allMarkings, CharacterPalette palette, IRobustRandom random)
    {
        if (layerLimits is null)
            return new();

        if (layer == HumanoidVisualLayers.Hair ||
            layer == HumanoidVisualLayers.FacialHair)
        {
            /* TODO: we should log an error here if using default, but Humanoid is full of static methods so we cant use sawmill until thats fixed
            if (!palette.ContainsKey(HairColorKey))
                sawmill.Error($"Palette for {layer} contains no HairColorKey, using default colour");
            */

            return PickHairsRandomMarking(layer, layerLimits, allMarkings, palette.HairColor, random);
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
                    palette.SkinColor,
                    palette.EyeColor,
                    outMarkings)
                    : random.Pick(new List<Color>
                    {
                        palette.HairColor,
                        palette.EyeColor
                    });

                colors.Add(color);
            }

            outMarkings.Add(new Marking(protoToAdd, colors));
        }
        return outMarkings;
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
    #endregion
}
